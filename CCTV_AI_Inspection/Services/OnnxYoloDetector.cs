using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics.Tensors;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using OpenCvSharp.Dnn; // (사용 안해도 되지만 향후 확장 대비)
using CCTV_AI_Inspection.Models;
using System.Runtime.InteropServices;

namespace CCTV_AI_Inspection.Services
{
    /// <summary>
    /// YOLOv5류 ONNX 모델을 CPU로 추론하는 최소 구현.
    /// - unsafe 미사용. OpenCV → Tensor 변환은 Marshal.Copy 사용
    /// - Letterbox(640x640), BGR→RGB, [0,1] 정규화
    /// - 출력 shape은 [1, N, 85] (xywh + obj + 80cls) 기준으로 작성. (필요시 수정)
    /// </summary>
    public class OnnxYoloDetector : IDisposable
    {
        private InferenceSession? _session;
        public bool IsLoaded => _session != null;

        public void Load(string onnxPath)
        {
            Dispose();
            var opt = new SessionOptions();
            opt.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
            // CPU만 사용. GPU 필요시: SessionOptions.MakeSessionOptionWithCudaProvider(deviceId) 사용. :contentReference[oaicite:4]{index=4}
            _session = new InferenceSession(onnxPath, opt);
        }

        public List<DetectionResult> Run(string imagePath, float conf = 0.25f, float nms = 0.45f, int inputSize = 640)
        {
            if (_session is null) throw new InvalidOperationException("모델이 로드되지 않았습니다.");

            using var src = Cv2.ImRead(imagePath, ImreadModes.Color);
            if (src.Empty()) throw new ArgumentException("이미지 로드 실패");

            // 1) Letterbox (패딩 포함 리사이즈) & 기록
            var (blob, ratio, padW, padH) = PreprocessToBlob(src, inputSize);

            // 2) 입력 이름/텐서 생성
            var inputName = _session.InputMetadata.Keys.First();
            var inputTensor = new DenseTensor<float>(blob, new[] { 1, 3, inputSize, inputSize });

            // 3) 추론
            using var results = _session.Run(new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(inputName, inputTensor) });
            
            // 보통 첫 번째 output 사용 (모델에 따라 이름/shape 다를 수 있음)
            var output = results.First().AsEnumerable<float>().ToArray();

            // 4) 후처리: 모델 shape 가정 [1, N, 85] → (x,y,w,h, conf, cls...)
            //    만약 [1, 85, N] 형태면 transpose 필수. (모델에 맞춰 수정)
            int n = output.Length / 85;
            var dets = new List<RawDet>(n);
            for (int i = 0; i < n; i++)
            {
                int o = i * 85;
                float x = output[o + 0];
                float y = output[o + 1];
                float w = output[o + 2];
                float h = output[o + 3];
                float obj = output[o + 4];

                // class score & id
                int cls = -1; float clsScore = 0f;
                for (int c = 5; c < 85; c++)
                {
                    var s = output[o + c];
                    if (s > clsScore) { clsScore = s; cls = c - 5; }
                }
                float score = obj * clsScore;
                if (score < conf) continue;

                // xywh → xyxy
                float left = x - w / 2f;
                float top = y - h / 2f;
                float right = x + w / 2f;
                float bottom = y + h / 2f;

                dets.Add(new RawDet { X1 = left, Y1 = top, X2 = right, Y2 = bottom, Score = score, ClassId = cls });
            }

            // 5) Letterbox 역보정 → 원본 좌표로 복원
            foreach (var d in dets)
            {
                d.X1 = (d.X1 - padW) / ratio;
                d.Y1 = (d.Y1 - padH) / ratio;
                d.X2 = (d.X2 - padW) / ratio;
                d.Y2 = (d.Y2 - padH) / ratio;
                // 클리핑
                d.X1 = Math.Max(0, Math.Min(src.Width - 1, d.X1));
                d.Y1 = Math.Max(0, Math.Min(src.Height - 1, d.Y1));
                d.X2 = Math.Max(0, Math.Min(src.Width - 1, d.X2));
                d.Y2 = Math.Max(0, Math.Min(src.Height - 1, d.Y2));
            }

            // 6) NMS
            var picked = Nms(dets, (float)nms);

            // 7) View 바인딩용 DTO로 변환
            var detections = new List<DetectionResult>();
            foreach (var p in picked)
            {
                detections.Add(new DetectionResult
                {
                    Label = $"cls{p.ClassId}:{p.Score:0.00}",
                    Score = p.Score,
                    X = p.X1,
                    Y = p.Y1,
                    Width = p.X2 - p.X1,
                    Height = p.Y2 - p.Y1
                });
            }
            return detections;
        }

        /// <summary>
        /// OpenCV의 BGR 이미지를 Letterbox(정사각 640) → RGB → float[1,3,H,W] 텐서로 변환
        /// unsafe 미사용: Marshal.Copy로 채널별 복사
        /// </summary>
        private (float[] blob, float ratio, float padW, float padH) PreprocessToBlob(Mat src, int size)
        {
            // 비율 유지 리사이즈
            int w = src.Width, h = src.Height;
            float r = Math.Min((float)size / w, (float)size / h);
            int newW = (int)Math.Round(w * r);
            int newH = (int)Math.Round(h * r);

            using var resized = new Mat();
            Cv2.Resize(src, resized, new OpenCvSharp.Size(newW, newH));

            // 패딩
            int dw = size - newW;
            int dh = size - newH;
            int top = dh / 2, bottom = dh - top, left = dw / 2, right = dw - left;

            using var padded = new Mat();
            Cv2.CopyMakeBorder(resized, padded, top, bottom, left, right, BorderTypes.Constant, new Scalar(114, 114, 114));

            // BGR → RGB
            using var rgb = new Mat();
            Cv2.CvtColor(padded, rgb, ColorConversionCodes.BGR2RGB);

            // float32, 0~1 정규화
            using var rgb32f = new Mat();
            rgb.ConvertTo(rgb32f, MatType.CV_32FC3, 1.0 / 255.0);

            // HWC → CHW (채널 순서 분리 후 복사)
            int hw = size * size;
            var blob = new float[3 * hw];

            // 채널 분리
            var channels = Cv2.Split(rgb32f); // R,G,B (현재 rgb이므로 R,G,B 순서)
            try
            {
                for (int c = 0; c < 3; c++)
                {
                    var ch = channels[c];
                    var data = new float[hw];
                    Marshal.Copy(ch.Data, data, 0, hw);
                    Array.Copy(data, 0, blob, c * hw, hw);
                    ch.Dispose();
                }
            }
            finally
            {
                foreach (var ch in channels) ch.Dispose();
            }

            return (blob, r, left, top);
        }

        private sealed class RawDet
        {
            public float X1, Y1, X2, Y2, Score;
            public int ClassId;
        }

        private static List<RawDet> Nms(List<RawDet> dets, float iouThresh)
        {
            var sorted = dets.OrderByDescending(d => d.Score).ToList();
            var picked = new List<RawDet>();

            while (sorted.Count > 0)
            {
                var best = sorted[0];
                picked.Add(best);
                sorted.RemoveAt(0);

                sorted = sorted.Where(d => IoU(best, d) < iouThresh).ToList();
            }
            return picked;
        }

        private static float IoU(RawDet a, RawDet b)
        {
            float xx1 = Math.Max(a.X1, b.X1);
            float yy1 = Math.Max(a.Y1, b.Y1);
            float xx2 = Math.Min(a.X2, b.X2);
            float yy2 = Math.Min(a.Y2, b.Y2);

            float w = Math.Max(0, xx2 - xx1);
            float h = Math.Max(0, yy2 - yy1);
            float inter = w * h;

            float areaA = (a.X2 - a.X1) * (a.Y2 - a.Y1);
            float areaB = (b.X2 - b.X1) * (b.Y2 - b.Y1);
            float union = areaA + areaB - inter;

            return union <= 0 ? 0 : inter / union;
        }

        public void Dispose()
        {
            _session?.Dispose();
            _session = null;
        }
    }
}
