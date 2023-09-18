using _3DViewer.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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

        public MainVM()
        {
            int w = 1500;
            int h = 2000;
            // длину и ширину по-хорошему нужно получать в рантайме
            _bitmap = new(w, h, 96, 96, PixelFormats.Bgr32, null);

            _objVertices.ParseObj("D:\\7_sem\\AKG\\untitled.obj");
            _bitmapGenerator = new BitmapGenerator(_objVertices, w, h);

            byte[,,] btm = _bitmapGenerator.GenerateImage();

            Bitmap.WritePixels(new Int32Rect(0,0, w, h), btm.Cast<byte>().ToArray(), w*btm.GetLength(2), 0);
            Bitmap = Bitmap.Clone();

            MouseDownCommand = new RelayCommand(MouseDown);
            MouseUpCommand = new RelayCommand(MouseUp);
            MouseMoveCommand = new RelayCommand(MouseMove);
        }

        public void MouseDown()
        {

        }
        public void MouseUp()
        {

        }
        public void MouseMove()
        {

        }
    }
}
