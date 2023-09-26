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

        private float scale = 0.5f;
        private float sensitivity = 0.7f;

        private int w = 2000;
        private int h = 1000;

        private float rotationX = 0;
        private float rotationY = 0;
        private float rotationZ = 0;


        private float translationX = 0;
        private float translationY = 0;
        private float translationZ = 0;

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

            var cat = Resource.cat;
            MemoryStream stream = new ();
            stream.Write(cat, 0, cat.Length);

            _objVertices.ParseObj(stream);
            _bitmapGenerator = new BitmapGenerator(_objVertices, w, h);
            Render();
            MouseDownCommand = new RelayCommand<Point>((point) => MouseDown(point));
            MouseUpCommand = new RelayCommand(MouseUp);
            MouseMoveCommand = new RelayCommand<Point>((point) => MouseMove(point));
            MouseWheelCommand = new RelayCommand<int>((delta) => MouseWheel(delta));

            KeyDownCommand = new RelayCommand<Key>((key) => KeyDown(key));
        }

        private void Render()
        {
            byte[,,] btm = _bitmapGenerator.GenerateImage(rotationX, rotationY, rotationZ, translationX, translationY, translationZ, scale);
            Bitmap.WritePixels(new Int32Rect(0, 0, w, h), btm.Cast<byte>().ToArray(), w * 4, 0);
        }

        private void KeyDown(Key key)
        {
            float rotMin = (float)(Math.PI / 16);
            float transMin = 5.0f;

            switch (key)
            {
                case Key.Left:
                    rotationY -= rotMin;
                    break;
                case Key.Right:
                    rotationY += rotMin;
                    break;
                case Key.Up:
                    rotationX -= rotMin;
                    break;
                case Key.Down:
                    rotationX += rotMin;
                    break;
                case Key.OemComma:
                    rotationZ -= rotMin;
                    break;
                case Key.OemPeriod:
                    rotationZ += rotMin;
                    break;

                case Key.W:
                    translationY += transMin;
                    break;
                case Key.A:
                    translationX -= transMin;
                    break;
                case Key.S:
                    translationY -= transMin;
                    break;
                case Key.D:
                    translationX += transMin;
                    break;
            }

            rotationX %= (float)Math.PI * 2;
            rotationY %= (float)Math.PI * 2;
            rotationZ %= (float)Math.PI * 2;

            Render();
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
            if(delta > 0)
            {
                scale /= sensitivity;
            }
            else
            {
                scale *= sensitivity;
            }

            Render();
        }
    }
}
