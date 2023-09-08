using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace _3DViewer.View
{
    public class MainVM : ObservableObject
    {
        private WriteableBitmap _bitmap;
        private Image _image = new();
        public Image Image
        {
            get { return _image; }
            set 
            { 
                _image = value; 
                OnPropertyChanged();
            }
        }

        public ICommand MouseDownCommand { get; }
        public ICommand MouseUpCommand { get; }
        public ICommand MouseMoveCommand { get; }

        public MainVM()
        {
            // длину и ширину по-хорошему нужно получать в рантайме
            _bitmap = new(800, 450, 96, 96, PixelFormats.Bgr32, null);
            _image.Source = _bitmap;

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
