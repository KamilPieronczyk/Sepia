using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
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
using LiveCharts;
using LiveCharts.Wpf;
using System.Runtime.CompilerServices;
using LiveCharts.Helpers;

namespace Sepia
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        public SeriesCollection SeriesCollection { get; set; }
        public SeriesCollection SeriesCollectionAfter { get; set; }
        Bitmap bmp;
        Bitmap sepiaBmp;

        //[DllImport(@"C:\Users\Kamil\source\repos\Sepia\x64\Debug\AsmDll.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern uint MyProc1();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string libname);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool FreeLibrary(IntPtr hModule);
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        delegate void CreateSepia_Delegate(byte[] bytes, int length, int deepth);

        CreateSepia_Delegate CreateSepia;

        public MainWindow()
        {
            InitializeComponent();
            InitHistogram();
            //CreateSepia = LoadFromCs;
            CreateSepia = LoadFromAsm;
        }

        void LoadFromAsm(byte[] bytes, int length, int deepth)
        {            
            IntPtr Handle = LoadLibrary(@"C:\Users\Kamil\source\repos\Sepia\x64\Debug\AsmDll.dll");
            IntPtr funcaddr = GetProcAddress(Handle, "CreateSepia");
            CreateSepia_Delegate function = Marshal.GetDelegateForFunctionPointer(funcaddr, typeof(CreateSepia_Delegate)) as CreateSepia_Delegate;
            function.Invoke(bytes, length , deepth);
        }

        void LoadFromCs(byte[] bytes, int length, int deepth)
        {
            var DLL = Assembly.LoadFile(@"C:\Users\Kamil\source\repos\Sepia\CsDll\bin\Debug\CsDll.dll");
            var class1Type = DLL.GetType("CsDll.Class1");
            dynamic c = Activator.CreateInstance(class1Type);
            c.CreateSepia(bytes, length, deepth);
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
            int sepia = (int)sepia_deepth.Value;

            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData = sepiaBmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, sepiaBmp.PixelFormat);


            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int length = bmpData.Width * 3 * sepiaBmp.Height;

            //offset of the processor world
            int offset = Math.Abs(bmpData.Stride) - (sepiaBmp.Width * 3);

            byte[] rgbValues = new byte[length];

            // Copy the RGB values into the array.
            int pos = 0;
            for (int i = 0; i < sepiaBmp.Height * Math.Abs(bmpData.Stride); i+= sepiaBmp.Width*3)
            {
                System.Runtime.InteropServices.Marshal.Copy(ptr + i, rgbValues, pos, sepiaBmp.Width * 3);
                pos += sepiaBmp.Width * 3;
                i += offset;
            }

            CreateHistogram(rgbValues, SeriesCollection);

            //Time measure
            var watch = System.Diagnostics.Stopwatch.StartNew();
            //Run Sepia
            CreateSepia.Invoke(rgbValues, length, sepia);
            //Execution time
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            timeLabel.Content = elapsedMs.ToString();

            CreateHistogram(rgbValues, SeriesCollectionAfter);


            // Copy the RGB values back to the bitmap
            pos = 0;
            for (int i = 0; i < sepiaBmp.Height * Math.Abs(bmpData.Stride); i += sepiaBmp.Width * 3)
            {
                System.Runtime.InteropServices.Marshal.Copy(rgbValues, pos, ptr + i, sepiaBmp.Width * 3);
                pos += sepiaBmp.Width * 3;
                i += offset;
            }



            // Unlock the bits.
            sepiaBmp.UnlockBits(bmpData);



        }

        private void Save_Image()
        {
        }

        private void Button_Generate(object sender, RoutedEventArgs e)
        {
            if (AsmRadio.IsChecked == true)
                CreateSepia = LoadFromAsm;
            else
                CreateSepia = LoadFromCs;

            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
            sepiaBmp = bmp.Clone(rect, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Sepia();
            SepiaImage.Source = ImageSourceFromBitmap(sepiaBmp);
            sepiaBmp.Save(@"C:\Users\Kamil\Desktop\output.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

        }

        private void CreateHistogram(byte[] rgbValues, SeriesCollection series)
        {
            int[] R = new int[256];
            int[] G = new int[256];
            int[] B = new int[256];

            series[0].Values.Clear();
            series[1].Values.Clear();
            series[2].Values.Clear();

            for (int i = 0; i < 256; i++)
            {
                R[i] = 0;
                G[i] = 0;
                B[i] = 0;
            }

            for(int i = 0; i < rgbValues.Length; i += 3)
            {
                R[(int)rgbValues[i]]++;
                G[(int)rgbValues[i + 1]]++;
                B[(int)rgbValues[i + 2]]++;
            }

            for (int i = 0; i < 256; i++)
            {
                series[0].Values.Add(R[i]);
                series[1].Values.Add(G[i]);
                series[2].Values.Add(B[i]);
            }
        }

        private void InitHistogram()
        {
            chart.AxisY.Clear();
            chart.AxisY.Add(
                new Axis
                {
                    MinValue = 0
                }
            );

            chartAfter.AxisY.Clear();
            chartAfter.AxisY.Add(
                new Axis
                {
                    MinValue = 0
                }
            );

            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Red",
                    PointGeometry = null,
                    Values = new ChartValues<int>{},
                    Stroke = System.Windows.Media.Brushes.Red
                },
                new LineSeries
                {
                    Title = "Green",
                    PointGeometry = null,
                    Values = new ChartValues<int>{},
                    Stroke = System.Windows.Media.Brushes.Green
                },
                new LineSeries
                {
                    Title = "Blue",
                    PointGeometry = null,
                    Values = new ChartValues<int>{},
                    Stroke = System.Windows.Media.Brushes.Blue
                }
            };

            SeriesCollectionAfter = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Red",
                    PointGeometry = null,
                    Values = new ChartValues<int>{},
                    Stroke = System.Windows.Media.Brushes.Red
                },
                new LineSeries
                {
                    Title = "Green",
                    PointGeometry = null,
                    Values = new ChartValues<int>{},
                    Stroke = System.Windows.Media.Brushes.Green
                },
                new LineSeries
                {
                    Title = "Blue",
                    PointGeometry = null,
                    Values = new ChartValues<int>{},
                    Stroke = System.Windows.Media.Brushes.Blue
                }
            };


            DataContext = this;
        }
    }
}
