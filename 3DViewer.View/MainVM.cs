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
using _3DViewer.Core.obj_parse;

namespace _3DViewer.View
{
    public class MainVM : ObservableObject, INotifyPropertyChanged
    {
        private string basePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Model");

        private Pbgra32Bitmap _pbgra32;
        private WriteableBitmap _bitmap;

        private readonly ResourcesStreams _resourcesStreams = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Model"));
        private readonly ObjVertices _objVertices = new();
        private readonly MtlInformation _mtlInformation = new MtlInformation();

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
            _objVertices = _resourcesStreams.GetVertices();
            _mtlInformation = _resourcesStreams.GetMtlInformation(_objVertices);
            foreach(MtlCharacter mtlCharacter in _mtlInformation.mtlCharacters)
            {
                if(mtlCharacter.mapKa != null)
                {
                    mtlCharacter.kaImage = ReadMap(
                        mtlCharacter.mapKa,
                        out mtlCharacter._widthKa,
                        out mtlCharacter._heightKa
                        );
                }
                if (mtlCharacter.mapKd != null)
                {
                    mtlCharacter.kdImage = ReadMap(
                        mtlCharacter.mapKd,
                        out mtlCharacter._widthKd,
                        out mtlCharacter._heightKd
                        );
                }
                if (mtlCharacter.mapKs != null)
                {
                    mtlCharacter.ksImage = ReadMap(
                        mtlCharacter.mapKs,
                        out mtlCharacter._widthKs,
                        out mtlCharacter._heightKs
                        );
                }

                if (mtlCharacter.norm != null)
                {
                    mtlCharacter.normImage = ReadMap(
                        mtlCharacter.norm,
                        out mtlCharacter._widthNorm,
                        out mtlCharacter._heightNorm
                        );
                }
            }

            Bitmap = new(_width, _height, 96, 96, PixelFormats.Pbgra32, null);
            _bitmapGenerator = new BitmapGenerator(_objVertices, _mtlInformation, _width, _height);

            _pbgra32 = new Pbgra32Bitmap(Bitmap);

            MouseDownCommand = new RelayCommand<Point>((point) => MouseDown(point));
            MouseUpCommand = new RelayCommand(MouseUp);
            MouseMoveCommand = new RelayCommand<Point>((point) => MouseMove(point));
            MouseWheelCommand = new RelayCommand<int>((delta) => MouseWheel(delta));
            SizeChangedCommand = new RelayCommand<Size>((size) => SizeChanged(size));
        }

        public byte[] ReadMap(string relativePath, out int width, out int height)
        {
            string mtlPath = Path.Combine(basePath, relativePath);
            byte[] res = Array.Empty<byte>();

            width = 0;
            height = 0; 

            if (File.Exists(mtlPath))
            {
                using (FileStream stream = File.OpenRead(mtlPath))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    BitmapFrame bitmapFrame = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    WriteableBitmap currBtm = new(new FormatConvertedBitmap(bitmapFrame, PixelFormats.Pbgra32, null, 0));

                    width = currBtm.PixelWidth;
                    height = currBtm.PixelHeight;

                    res = new byte[currBtm.PixelWidth * currBtm.PixelHeight * 4];
                    currBtm.CopyPixels(res, currBtm.PixelWidth * 4, 0);
                }
            }
            else
            {

            }
            return res;
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
            if (FrameRate > MaxFrameRate)
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
