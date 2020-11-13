/**
 * @author Kamil Pierończyk Gr.2 AEI, Inf, rok 3
 * Sepia
 */
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
using System.Threading;
using System.Threading.Tasks;

namespace Sepia
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    /// 
    delegate void CreateSepia_Delegate(byte[] bytes, int length, int deepth);
    public partial class MainWindow : Window
    {
        public SeriesCollection SeriesCollection { get; set; }
        public SeriesCollection SeriesCollectionAfter { get; set; }
        Bitmap bmp; //bitmapa zdjecia do edycji
        Bitmap sepiaBmp; //bitmapa zdjecia po edycji

        //biblioteki do ładowania dll dynamicznie
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string libname);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool FreeLibrary(IntPtr hModule);
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);


        CreateSepia_Delegate CreateSepia; //wskaźnik na funckję wykonywującą algorytm

        public MainWindow()
        {
            InitializeComponent();
            InitHistogram(); //przygotowuje wykresy histogramu
        }

        //Ładuje biblioteke asm
        void LoadFromAsm(byte[] bytes, int length, int deepth)
        {   
            IntPtr Handle = LoadLibrary(@"./AsmDll.dll");
            IntPtr funcaddr = GetProcAddress(Handle, "CreateSepia");
            CreateSepia_Delegate function = Marshal.GetDelegateForFunctionPointer(funcaddr, typeof(CreateSepia_Delegate)) as CreateSepia_Delegate;
            function.Invoke(bytes, length , deepth);
        }

        //Ładuje biblioteke cs
        void LoadFromCs(byte[] bytes, int length, int deepth)
        {
            //var DLL = Assembly.LoadFile(@"C:\Users\Kamil\source\repos\Sepia\CsDll\bin\Debug\CsDll.dll");
            var dllFile = new FileInfo(@"./CsDll.dll");
            var DLL = Assembly.LoadFile(dllFile.FullName);
            var class1Type = DLL.GetType("CsDll.Class1");
            dynamic c = Activator.CreateInstance(class1Type);
            c.CreateSepia(bytes, length, deepth);
        }

        //Funkcja przycisku do załadowania zdjęcia
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

        //Tworzy klasę ImageSource do wyświetlania zdjęcia z bitmapy
        public ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }

        //Uruchamia algorytm
        public void Sepia()
        {
            int sepia = (int)sepia_deepth.Value;

            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
            sepiaBmp = bmp.Clone(rect, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
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

            int threadsCount = (int)threads.Value;
            var threadsArray = new List<Task>();
            byte[][] arrayContainer = new byte[threadsCount][];

            int SPAN = (int)Math.Ceiling(((decimal)sepiaBmp.Height / threadsCount)) * sepiaBmp.Width * 3;
            int span = SPAN;


            //Time measure
            var watch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < threadsCount; i++)
            {
                if (span * (i + 1) > length) span = length - (span * i);
                arrayContainer[i] = new byte[span];
                Array.Copy(rgbValues, SPAN * i, arrayContainer[i], 0, span);
                SepiaThread sepiaThread = new SepiaThread(arrayContainer[i], span, (int)sepia_deepth.Value, CreateSepia);
                threadsArray.Add(Task.Factory.StartNew(sepiaThread.Sepia));
            }

            Task.WaitAll(threadsArray.ToArray());

            //Execution time
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            timeLabel.Content = elapsedMs.ToString();

            span = (int)Math.Ceiling(((decimal)sepiaBmp.Height / threadsCount)) * sepiaBmp.Width * 3;
            for (int i = 0; i < threadsCount; i++)
            {
                if (span * (i + 1) > length) span = length - (span * i);
                Array.Copy(arrayContainer[i], 0, rgbValues, SPAN * i, span);
            }
            

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

            SepiaImage.Source = ImageSourceFromBitmap(sepiaBmp);
        }

        //Zapisuje zdjęcie
        private void Save_Image(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "JPG (*.jpg)|*.jpg|PNG (*.png)|*.png";
            if (saveFileDialog.ShowDialog() == true)
            {
                var fileName = saveFileDialog.FileName;
                var extension = System.IO.Path.GetExtension(saveFileDialog.FileName);

                switch (extension.ToLower())
                {
                    case ".jpg":
                        sepiaBmp.Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                        break;
                    case ".png":
                        sepiaBmp.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(extension);
                }
            }
        }

        //Przycisk uruchamiający algorytm
        private void Button_Generate(object sender, RoutedEventArgs e)
        {
            if (AsmRadio.IsChecked == true)
                CreateSepia = LoadFromAsm;
            else
                CreateSepia = LoadFromCs;

            Sepia();

        }

        //Tworzy histogram
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

        //Przygotowuje wykresy
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

    //Służy do wywołania algorytmu z wątkami
    class SepiaThread
    {
        byte[] rgbValues;
        int length;
        int sepiaDeeth;
        CreateSepia_Delegate CreateSepia;

        public SepiaThread(byte[] rgbValues, int length, int sepiaDeeth, CreateSepia_Delegate createSepia)
        {
            this.rgbValues = rgbValues;
            this.length = length;
            this.sepiaDeeth = sepiaDeeth;
            CreateSepia = createSepia;
        }

        public void Sepia()
        {
            CreateSepia.Invoke(rgbValues, length, sepiaDeeth);
        }
    }
}
