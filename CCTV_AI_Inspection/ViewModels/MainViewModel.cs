using CCTV_AI_Inspection.Models;
using CCTV_AI_Inspection.Services;
using CCTV_AI_Inspection.Utils;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using ToolkitRelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand;


namespace CCTV_AI_Inspection.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // 바인딩용 프로퍼티
        private string _imagePath;
        private string _modelPath;
        private BitmapSource _displayImage;
        private string _status;
        private double _confThreshold = 0.25;
        private double _nmsThreshold = 0.45;

        public string ImagePath { get => _imagePath; set { _imagePath = value; OnPropertyChanged(); } }
        public string ModelPath { get => _modelPath; set { _modelPath = value; OnPropertyChanged(); } }
        public BitmapSource DisplayImage { get => _displayImage; set { _displayImage = value; OnPropertyChanged(); } }
        public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }
        public double ConfThreshold { get => _confThreshold; set { _confThreshold = value; OnPropertyChanged(); } }
        public double NmsThreshold { get => _nmsThreshold; set { _nmsThreshold = value; OnPropertyChanged(); } }

        public ObservableCollection<DetectionResult> Results { get; } = new();

        // 명령
        public ToolkitRelayCommand OpenImageCommand { get; }
        public ToolkitRelayCommand OpenModelCommand { get; }
        public ToolkitRelayCommand InferenceCommand { get; }

        private readonly OnnxYoloDetector _detector = new();

        public MainViewModel()
        {
            OpenImageCommand = new ToolkitRelayCommand(OpenImage);
            OpenModelCommand = new ToolkitRelayCommand(OpenModel);
            InferenceCommand = new ToolkitRelayCommand(Inference, CanRun);
        }

        private void OpenImage()
        {
            var dlg = new OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp" };
            if (dlg.ShowDialog() == true)
            {
                ImagePath = dlg.FileName;
                DisplayImage = ImageUtils.LoadBitmap(ImagePath);
                Results.Clear();
                Status = "이미지 로드 완료";
            }
        }

        private void OpenModel()
        {
            var dlg = new OpenFileDialog { Filter = "ONNX Model|*.onnx" };
            if (dlg.ShowDialog() == true)
            {
                ModelPath = dlg.FileName;
                try
                {
                    _detector.Load(ModelPath);
                    Status = "모델 로드 완료";
                }
                catch (Exception ex)
                {
                    Status = $"모델 로드 실패: {ex.Message}";
                }
            }
        }

        private bool CanRun() => !string.IsNullOrEmpty(ImagePath) && _detector.IsLoaded;

        private void Inference()
        {
            try
            {
                Results.Clear();

                var results = _detector.Run(ImagePath, (float)ConfThreshold, (float)NmsThreshold, 640);
                // 결과 채우기 (View에서 Canvas 좌표계로 사용)
                foreach (var r in results)
                    Results.Add(r);

                // 결과 이미지 갱신 (원본 그대로 + Canvas 오버레이)
                DisplayImage = ImageUtils.LoadBitmap(ImagePath);

                Status = $"추론 완료: {Results.Count} 개";
            }
            catch (Exception ex)
            {
                Status = $"추론 실패: {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

