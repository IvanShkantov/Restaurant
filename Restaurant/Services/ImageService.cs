using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Restaurant.Services
{
    static class ImageService
    {
        public static string ChooseImage(string imgFolder, Image imageControl)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Images|*.png;*.jpg;*.jpeg";

            if (dlg.ShowDialog() == true)
            {
                string sourcePath = dlg.FileName;
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string destFolder = Path.Combine(baseDir, "img", imgFolder);

                Directory.CreateDirectory(destFolder);

                string fileName = Path.GetFileName(sourcePath);
                string destPath = Path.Combine(destFolder, fileName);

                byte[] fileBytes;
                using (var fs = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    fileBytes = new byte[fs.Length];
                    fs.Read(fileBytes, 0, fileBytes.Length);
                }

                if (File.Exists(destPath))
                {
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    string ext = Path.GetExtension(fileName);
                    fileName = $"{nameWithoutExt}_{Guid.NewGuid():N}{ext}";
                    destPath = Path.Combine(destFolder, fileName);
                }

                File.WriteAllBytes(destPath, fileBytes);

                UpdateImageFromBytes(fileBytes, imageControl);
                return $"img\\{imgFolder}\\{fileName}";
            }
            return null;
        }

        public static void UpdateImageFromBytes(byte[] imageBytes, Image image)
        {
            var bitmap = new BitmapImage();
            using (var stream = new MemoryStream(imageBytes))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
            }
            bitmap.Freeze();

            image.Source = bitmap;
        }
    }
}

namespace Restaurant.Product
{
    public class ImagePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string path = value as string;
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            if (string.IsNullOrWhiteSpace(path))
            {
                return Path.Combine(baseDir, "img", "no-image.png");
            }

            string fullPath = Path.Combine(baseDir, path);

            if (!File.Exists(fullPath))
            {
                return Path.Combine(baseDir, "img", "no-image.png");
            }

            return fullPath;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

}