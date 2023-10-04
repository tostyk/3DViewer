using _3DViewer.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace _3DViewer.View
{
    public class MainVM : ObservableObject, INotifyPropertyChanged
    {
        private WriteableBitmap _bitmap;

        private readonly ObjVertices _objVertices = new();
        private readonly BitmapGenerator _bitmapGenerator;
        private bool _rotation = false;
        private Point _prevPoint;

        private int _width = 2000;
        private int _height;

        //private double _quality = 1.5;

        private bool dontTouch = false;
        private float sensitivity = 0.001f;

        byte[] btm;

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
            var obj = Resource.cat_1;

            MemoryStream stream = new();
            stream.Write(obj, 0, obj.Length);

            _objVertices.ParseObj(stream);
            _bitmapGenerator = new BitmapGenerator(_objVertices, _width, _height);
            Bitmap = new(_width, _height, 96, 96, PixelFormats.Bgr32, null);

            DrawNewFrame();

            MouseDownCommand = new RelayCommand<Point>((point) => MouseDown(point));
            MouseUpCommand = new RelayCommand(MouseUp);
            MouseMoveCommand = new RelayCommand<Point>((point) => MouseMove(point));
            MouseWheelCommand = new RelayCommand<int>((delta) => MouseWheel(delta));
            SizeChangedCommand = new RelayCommand<Size>((size) => SizeChanged(size));
        }

        private void SizeChanged(Size size)
        {
            //_width = Convert.ToInt32(size.Width * _quality);
            _height = Convert.ToInt32(size.Height / size.Width * _width);// * _quality);

            _bitmapGenerator.Resized(_width, _height);
            Bitmap = new(_width, _height, 96, 96, PixelFormats.Bgr32, null);

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
            if (_rotation && !dontTouch)
            {

                _bitmapGenerator.ReplaceCameraByScreenCoordinates(
                    (float)point.X,
                    (float)point.Y,
                    (float)_prevPoint.X,
                    (float)_prevPoint.Y
                    );
                _prevPoint = point;
                DrawNewFrame();
                

               // ReplaceCursor(Convert.ToInt32(point.X), Convert.ToInt32(point.Y));

            }
            else
            {
                dontTouch = false;
            }
        }

        private void MouseWheel(int delta)
        {
            _bitmapGenerator.Scale(delta*sensitivity);
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
        private void DrawNewFrame()
        {
            _stopwatch.Restart();
            btm = _bitmapGenerator.GenerateImage();
            Bitmap.Lock();
            Bitmap.WritePixels(new Int32Rect(0, 0, _width, _height), btm, _width * 4, 0);
            Bitmap.Unlock();
            FrameRate = 1000 * 10_000 / _stopwatch.ElapsedTicks;
        }
    }
}
