using _3DViewer.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;
using System.Linq;
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

        private ObjVertices _objVertices = new ObjVertices();
        private BitmapGenerator _bitmapGenerator;
        private bool _rotation = false;
        private Point _prevPoint;

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
        public MainVM()
        {
            int w = 2500;
            int h = 2000;
            // длину и ширину по-хорошему нужно получать в рантайме
            _bitmap = new(w, h, 96, 96, PixelFormats.Bgr32, null);

            _objVertices.ParseObj("sphere.obj");
            _bitmapGenerator = new BitmapGenerator(_objVertices, w, h);

            byte[,,] btm = _bitmapGenerator.GenerateImage();
            Bitmap.WritePixels(new Int32Rect(0, 0, w, h), btm.Cast<byte>().ToArray(), w*4, 0);
            //Bitmap = Bitmap.Clone();

            MouseDownCommand = new RelayCommand<Point>((point) => MouseDown(point));
            MouseUpCommand = new RelayCommand(MouseUp);
            MouseMoveCommand = new RelayCommand<Point>((point) => MouseMove(point));
            MouseWheelCommand = new RelayCommand<int>((delta) => MouseWheel(delta));
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
            //int w = 2500;
            //int h = 2000;
            //byte[,,] btm = _bitmapGenerator.GenerateImage(100 + delta);

            //Bitmap.WritePixels(new Int32Rect(0, 0, w, h), btm.Cast<byte>().ToArray(), w * 4, 0);
        }
    }
}
