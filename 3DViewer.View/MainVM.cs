using _3DViewer.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
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
    public class MainVM : ObservableObject
    {
        private WriteableBitmap _bitmap;

        private ObjVertices _objVertices = new();
        private BitmapGenerator _bitmapGenerator;
        private bool _rotation = false;
        private Point _prevPoint;

        private float sensitivity = 0.7f;

        private int _width = 2000;
        private int _height = 1000;

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

        public MainVM()
        {
            // длину и ширину по-хорошему нужно получать в рантайме
            _bitmap = new(_width, _height, 96, 96, PixelFormats.Bgr32, null);

            var obj = Resource.cat;
            MemoryStream stream = new();
            stream.Write(obj, 0, obj.Length);

            _objVertices.ParseObj(stream);
            _bitmapGenerator = new BitmapGenerator(_objVertices, _width, _height);

            DrawNewFrame();

            MouseDownCommand = new RelayCommand<Point>((point) => MouseDown(point));
            MouseUpCommand = new RelayCommand(MouseUp);
            MouseMoveCommand = new RelayCommand<Point>((point) => MouseMove(point));
            MouseWheelCommand = new RelayCommand<int>((delta) => MouseWheel(delta));
        }


        //private void KeyDown(Key key)
        //{
        //    float rotMin = (float)(Math.PI / 36);
        //    float transMin = 125.0f;
        //    float cameraDelta = 0.5f;

        //    switch (key)
        //    {
        //        //case Key.Left:
        //        //    _bitmapGenerator.Rotate(-rotMin, 0, 0);
        //        //    break;
        //        //case Key.Right:
        //        //    _bitmapGenerator.Rotate(rotMin, 0, 0);
        //        //    break;
        //        //case Key.Up:
        //        //    _bitmapGenerator.Rotate(0, -rotMin, 0);
        //        //    break;
        //        //case Key.Down:
        //        //    _bitmapGenerator.Rotate(0, rotMin, 0);
        //        //    break;
        //        //case Key.OemComma:
        //        //    _bitmapGenerator.Rotate(0, 0, -rotMin);
        //        //    break;
        //        //case Key.OemPeriod:
        //        //    _bitmapGenerator.Rotate(0, 0, rotMin);
        //        //    break;

        //        //case Key.W:
        //        //    _bitmapGenerator.Translate(0, -transMin, 0);
        //        //    break;
        //        //case Key.A:
        //        //    _bitmapGenerator.Translate(-transMin, 0, 0);
        //        //    break;
        //        //case Key.S:
        //        //    _bitmapGenerator.Translate(0, transMin, 0);
        //        //    break;
        //        //case Key.D:
        //        //    _bitmapGenerator.Translate(transMin, 0, 0);
        //        //    break;
        //        //case Key.E:
        //        //    _bitmapGenerator.Translate(0, 0, -transMin);
        //        //    break;
        //        //case Key.Q:
        //        //    _bitmapGenerator.Translate(0, 0, transMin);
        //        //    break;

        //        case Key.OemMinus:
        //            _bitmapGenerator.Scale(sensitivity);
        //            break;
        //        case Key.OemPlus:
        //            _bitmapGenerator.Scale(-sensitivity);
        //            break;

        //        case Key.NumPad2:
        //            _bitmapGenerator.ChangeCameraPosition(0, 0, -cameraDelta);
        //            break;
        //        case Key.NumPad8:
        //            _bitmapGenerator.ChangeCameraPosition(0, 0, cameraDelta);
        //            break;
        //        case Key.NumPad4:
        //            _bitmapGenerator.ChangeCameraPosition(-cameraDelta, 0, 0);
        //            break;
        //        case Key.NumPad6:
        //            _bitmapGenerator.ChangeCameraPosition(cameraDelta, 0, 0);
        //            break;
        //        case Key.NumPad7:
        //            _bitmapGenerator.ChangeCameraPosition(0, cameraDelta, 0);
        //            break;
        //        case Key.NumPad9:
        //            _bitmapGenerator.ChangeCameraPosition(0, -cameraDelta, 0);
        //            break;

        //    }
        //    btm = _bitmapGenerator.GenerateImage();
        //    Bitmap.Lock();
        //    Bitmap.WritePixels(new Int32Rect(0, 0, _width, _height), btm, _width * 4, 0);
        //    Bitmap.Unlock();
        //}

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
            _bitmapGenerator.Scale(delta / 20);
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
