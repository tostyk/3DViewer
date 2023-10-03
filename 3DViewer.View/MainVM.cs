﻿using _3DViewer.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace _3DViewer.View
{
    public class MainVM : ObservableObject, INotifyPropertyChanged
    {

        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);


        private WriteableBitmap _bitmap;

        private readonly ObjVertices _objVertices = new();
        private readonly BitmapGenerator _bitmapGenerator;
        private bool _rotation = false;
        private Point _prevPoint;

        private int _width = 990;
        private int _height = 1000;


        private double _imgWidth;
        private double _imgHeight;

        private bool dontTouch = false;

        byte[] btm;

        public event PropertyChangedEventHandler PropertyChanged;

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
        public double ImgWidth
        {
            get { return _imgWidth; }
            set
            {
                _imgWidth = value;
                OnPropertyChanged("ImgWidth");
            }
        }

        public double ImgHeight
        {
            get { return _imgHeight; }
            set
            {
                _imgHeight = value;
                OnPropertyChanged("ImgHeight");
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
            double aspectRatio = size.Width / size.Height;
            _width = Convert.ToInt32(aspectRatio * _height);

            _bitmapGenerator.Resized(_width, _height);

            Bitmap = new(_width, _height, 96, 96, PixelFormats.Bgr32, null);

            btm = _bitmapGenerator.GenerateImage();

            Bitmap.Lock();
            Bitmap.WritePixels(new Int32Rect(0, 0, _width, _height), btm, _width * 4, 0);
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

        private void ReplaceCursor(int x, int y)
        {
            
            // Left boundary
            int xL = (int)App.Current.MainWindow.Left;
            // Right boundary
            int xR = (int)_imgWidth + xL;
            // Top boundary
            int yT = (int)App.Current.MainWindow.Top;
            // Bottom boundary
            int yB = (int)_imgHeight + yT;

            int prevX = x;
            int prevY = y;

            x += xL;
            y += yT;

            if (x < xL)
            {
                x = xR;
            }
            else if (x > xR)
            {
                x = xL;
            }

            if (y < yT)
            {
                y = yB;
            }
            else if (y > yB)
            {
                y = yT;
            }
            if(prevX != x - xL || prevY != y - yT)
            {
                dontTouch = true;
                SetCursorPos(x + xL, y + yT);
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

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
