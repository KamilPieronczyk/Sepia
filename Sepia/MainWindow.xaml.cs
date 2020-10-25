using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Color = System.Drawing.Color;

namespace Sepia
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Bitmap bmp;
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
                bmp = new Bitmap(fileName);
            }
        }

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        public ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }

        public void Sepia()
        {
            int x, y;
            for (x = 0; x < bmp.Width; x++)
            {
                for (y = 0; y < bmp.Height; y++)
                {
                    Color oldColor = bmp.GetPixel(x, y);
                    double newR = oldColor.R * 0.393 + oldColor.G * 0.769 + oldColor.B * 0.189;
                    double newG = 0.349 * oldColor.R + 0.686 * oldColor.G + 0.168 * oldColor.B;
                    double newB = 0.272 * oldColor.R + 0.534 * oldColor.G + 0.131 * oldColor.B;
                    byte R = newR > 255.0 ? (byte)255 : (byte)((int)newR);
                    byte G = newG > 255.0 ? (byte)255 : (byte)((int)newG);
                    byte B = newB > 255.0 ? (byte)255 : (byte)((int)newB);
                    Color newPixelColor = Color.FromArgb(R, G, B);
                    bmp.SetPixel(x, y, newPixelColor);                    
                }
            }
            SepiaImage.Source = ImageSourceFromBitmap(bmp);
        }

        private void Button_Generate(object sender, RoutedEventArgs e)
        {
            Sepia();
        }
    }
}
