using _3DViewer.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;

namespace _3DViewer.View
{
    public class MainVM : ObservableObject, INotifyPropertyChanged
    {
        private Pbgra32Bitmap _pbgra32;
        private WriteableBitmap _bitmap;

        private readonly ObjVertices _objVertices = new();
        private readonly BitmapGenerator _bitmapGenerator;
        private bool _rotation = false;
        private Point _prevPoint;

        private int _width = 2000;
        private int _height = 1000;
        private float sensitivity = 0.08f;
        byte[] btm;

        public event PropertyChangedEventHandler? PropertyChanged;

        static MainVM()
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
        }

        public WriteableBitmap Bitmap
        {
            get { return _bitmap; }
            set
            {
                _bitmap = value;
                OnPropertyChanged();
            }
        }
        public ICommand MouseDownCommand { get; }
        public ICommand MouseUpCommand { get; }
        public ICommand MouseMoveCommand { get; }
        public ICommand MouseWheelCommand { get; }
        public ICommand KeyDownCommand { get; }
        public ICommand SizeChangedCommand { get; }

        public MainVM()
        {
            var obj = Resource.spaceship;

            MemoryStream stream = new();
            stream.Write(obj, 0, obj.Length);

            _objVertices.ParseObj(stream);
            Bitmap = new(_width, _height, 96, 96, PixelFormats.Pbgra32, null);
            _bitmapGenerator = new BitmapGenerator(_objVertices, _width, _height);
            _pbgra32 = new Pbgra32Bitmap(Bitmap);

         //   DrawNewFrame();

            MouseDownCommand = new RelayCommand<Point>((point) => MouseDown(point));
            MouseUpCommand = new RelayCommand(MouseUp);
            MouseMoveCommand = new RelayCommand<Point>((point) => MouseMove(point));
            MouseWheelCommand = new RelayCommand<int>((delta) => MouseWheel(delta));
            SizeChangedCommand = new RelayCommand<Size>((size) => SizeChanged(size));
        }


        private void SizeChanged(Size size)
        {
            _height = Convert.ToInt32(size.Height / size.Width * _width);

            _bitmapGenerator.Resized(_width, _height);

            Bitmap = new(_width, _height, 96, 96, PixelFormats.Pbgra32, null);
            _pbgra32 = new Pbgra32Bitmap(Bitmap);
            Bitmap = _pbgra32.Source;

            DrawNewFrame();
        }
        

        private void MouseDown(Point point)
        {
            _rotation = !_rotation;
            if (_rotation)
            {
                _prevPoint = point;
            }
        }
        private void MouseUp()
        {
            _rotation = false;
        }
        private void MouseMove(Point point)
        {
            _rotation = Mouse.LeftButton == MouseButtonState.Pressed;
            if (_rotation)
            {
                _bitmapGenerator.ReplaceCameraByScreenCoordinates(
                    (float)point.X,
                    (float)point.Y,
                    (float)_prevPoint.X,
                    (float)_prevPoint.Y
                    );
                _prevPoint = point;
                DrawNewFrame();
            }
        }

        private void MouseWheel(int delta)
        {
            _bitmapGenerator.Scale(sensitivity * Math.Sign(delta));
            DrawNewFrame();
        }
        private readonly static Stopwatch _stopwatch = new();
        private long _frameRate = 0;
        public long FrameRate
        {
            get => _frameRate;
            set
            {
                _frameRate = value;


                OnPropertyChanged();
            }
        }
        private long _maxFrameRate = 0;
        public long MaxFrameRate
        {
            get => _maxFrameRate;
            set
            {
                _maxFrameRate = value;
                OnPropertyChanged();
            }
        }
        private long _avgFrameRate = 0;
        public long AvgFrameRate
        {
            get => _avgFrameRate;
            set
            {
                _avgFrameRate = value;
                OnPropertyChanged();
            }
        }

        private long _sumFps = 0;
        private long _numFrames = 0;

        private void DrawNewFrame()
        {
            _numFrames++;

            _stopwatch.Restart();
            Bitmap.Lock();
            btm = _bitmapGenerator.GenerateImage();
         // Marshal.Copy(btm, 0, Bitmap.BackBuffer, btm.Length);
         //   Bitmap.AddDirtyRect(new Int32Rect(0, 0, _width, _height));
            Bitmap.WritePixels(new Int32Rect(0, 0, _width, _height), btm, _width * 4, 0);
            Bitmap.Unlock();

            FrameRate = 1000 * 10_000 / _stopwatch.ElapsedTicks;
            if(FrameRate > MaxFrameRate)
            {
                MaxFrameRate = FrameRate;
            }
            _sumFps += FrameRate;
            AvgFrameRate = _sumFps / _numFrames;
        }
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
