using System.IO;
using System.Windows.Media.Imaging;

namespace CCTV_AI_Inspection.Utils
{
    public static class ImageUtils
    {
        public static BitmapSource LoadBitmap(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad; // 파일 핸들 잠금 방지
            bmp.StreamSource = fs;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
    }
}
