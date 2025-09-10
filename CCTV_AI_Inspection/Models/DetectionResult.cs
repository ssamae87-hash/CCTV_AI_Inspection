namespace CCTV_AI_Inspection.Models
{
    // View에서 Canvas Left/Top, Width/Height로 바인딩하기 위한 DTO
    public class DetectionResult
    {
        public string Label { get; set; } = "";
        public float Score { get; set; }
        public double X { get; set; }      // Canvas.Left
        public double Y { get; set; }      // Canvas.Top
        public double Width { get; set; }
        public double Height { get; set; }
    }
}