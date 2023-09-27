using _3DViewer.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using _3DViewer;
using System.Reflection;
using System.Resources;
using System.Runtime.Versioning;
using static System.Net.Mime.MediaTypeNames;

namespace _3DViewer.View
{
    public class MainVM : ObservableObject
    {
        private WriteableBitmap _bitmap;

        private ObjVertices _objVertices = new ObjVertices();
        private BitmapGenerator _bitmapGenerator;
        private bool _rotation = false;
        private Point _prevPoint;

        private float sensitivity = 0.7f;

        private int w = 2000;
        private int h = 1000;

        byte[,,] btm;

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
            _bitmap = new(w, h, 96, 96, PixelFormats.Bgr32, null);

            var cat = Resource.spider;
            MemoryStream stream = new MemoryStream();
            stream.Write(cat, 0, cat.Length);

            _objVertices.ParseObj(stream);
            _bitmapGenerator = new BitmapGenerator(_objVertices, w, h);
            btm = _bitmapGenerator.GenerateImage();
            Bitmap.Lock();
            Bitmap.WritePixels(new Int32Rect(0, 0, w, h), btm.Cast<byte>().ToArray(), w * 4, 0);
            Bitmap.Unlock();
            MouseDownCommand = new RelayCommand<Point>((point) => MouseDown(point));
            MouseUpCommand = new RelayCommand(MouseUp);
            MouseMoveCommand = new RelayCommand<Point>((point) => MouseMove(point));
            MouseWheelCommand = new RelayCommand<int>((delta) => MouseWheel(delta));

            KeyDownCommand = new RelayCommand<Key>((key) => KeyDown(key));
        }


        private void KeyDown(Key key)
        {
            float rotMin = (float)(Math.PI / 16);
            float transMin = 125.0f;
            float cameraDelta = 125.0f;

            switch (key)
            {
                case Key.Left:
                    btm = _bitmapGenerator.Rotate(-rotMin, 0, 0);
                    break;
                case Key.Right:
                    btm = _bitmapGenerator.Rotate(rotMin, 0, 0);
                    break;
                case Key.Up:
                    btm = _bitmapGenerator.Rotate(0, -rotMin, 0);
                    break;
                case Key.Down:
                    btm = _bitmapGenerator.Rotate(0, rotMin, 0);
                    break;
                case Key.OemComma:
                    btm = _bitmapGenerator.Rotate(0, 0, -rotMin);
                    break;
                case Key.OemPeriod:
                    btm = _bitmapGenerator.Rotate(0, 0, rotMin);
                    break;

                case Key.W:
                    btm = _bitmapGenerator.Translate(0, -transMin, 0);
                    break;
                case Key.A:
                    btm = _bitmapGenerator.Translate(-transMin, 0, 0);
                    break;
                case Key.S:
                    btm = _bitmapGenerator.Translate(0, transMin, 0);
                    break;
                case Key.D:
                    btm = _bitmapGenerator.Translate(transMin, 0, 0);
                    break;
                case Key.E:
                    btm = _bitmapGenerator.Translate(0, 0, -transMin);
                    break;
                case Key.Q:
                    btm = _bitmapGenerator.Translate(0, 0, transMin);
                    break;

                case Key.OemMinus:
                    btm = _bitmapGenerator.Scale(sensitivity);
                    break;
                case Key.OemPlus:
                    btm = _bitmapGenerator.Scale(1 / sensitivity);
                    break;

                case Key.NumPad2:
                    btm = _bitmapGenerator.ChangeCameraPosition(0, 0, -cameraDelta);
                    break;
                case Key.NumPad8:
                    btm = _bitmapGenerator.ChangeCameraPosition(0, 0, cameraDelta);
                    break;
                case Key.NumPad4:
                    btm = _bitmapGenerator.ChangeCameraPosition(-cameraDelta, 0, 0);
                    break;
                case Key.NumPad6:
                    btm = _bitmapGenerator.ChangeCameraPosition(cameraDelta, 0, 0);
                    break;
                case Key.NumPad7:
                    btm = _bitmapGenerator.ChangeCameraPosition(0, cameraDelta, 0);
                    break;
                case Key.NumPad9:
                    btm = _bitmapGenerator.ChangeCameraPosition(0, -cameraDelta, 0);
                    break;

            }
            Bitmap.Lock();
            Bitmap.WritePixels(new Int32Rect(0, 0, w, h), btm.Cast<byte>().ToArray(), w * 4, 0);
            Bitmap.Unlock();
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
            if (_rotation)
            {
                
            }
        }
        private void MouseWheel(int delta)
        {
           
        }
    }
}
