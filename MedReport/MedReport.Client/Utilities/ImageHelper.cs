using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace MedReport.Client.Utilities
{
    public static class ImageHelper
    {
        public static BitmapImage LoadThumbnailWithoutLocking(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return null;

            var bitmap = new BitmapImage();

            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filePath);
            bitmap.DecodePixelWidth = 150;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            if (bitmap.CanFreeze) bitmap.Freeze();

            return bitmap;
        }
    }
}