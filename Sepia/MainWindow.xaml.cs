using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sepia
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_LoadFromFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if(openFileDialog.ShowDialog() == true)
            {
                var fileName = openFileDialog.FileName;
                Uri fileUri = new Uri(fileName);
                SourceImage.Source = new BitmapImage(fileUri);
            }
        }

        public byte[] ConvertBitmapImageToByteArray(BitmapImage imageC)
        {
            MemoryStream memStream = new MemoryStream();
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(imageC));
            encoder.Save(memStream);
            return memStream.ToArray();
        }

        public BitmapImage ConvertByteArrayToBitmapImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        public void Sepia()
        {
            byte[] bmp = ConvertBitmapImageToByteArray(SourceImage.Source as BitmapImage);
            int i = 0;
            while (bmp.Length > i + 4)
            {
                double r = bmp[i + 1];
                double g = bmp[i + 2];
                double b = bmp[i + 3];
                double newR = r * 0.393 + b * 0.769 + g * 0.189;
                double newG = 0.349 * r + 0.686 * g + 0.168 * b;
                double newB = 0.272 * r + 0.534 * g + 0.131 * b;

                bmp[i + 1] = newR > 255.0 ? (byte)255 : (byte)((int)newR);
                bmp[i + 2] = newG > 255.0 ? (byte)255 : (byte)((int)newG);
                bmp[i + 3] = newB > 255.0 ? (byte)255 : (byte)((int)newB);
                i += 4;
            }
            SepiaImage.Source = ConvertByteArrayToBitmapImage(bmp);
        }

        private void Button_Generate(object sender, RoutedEventArgs e)
        {
            Sepia();
        }
    }
}
