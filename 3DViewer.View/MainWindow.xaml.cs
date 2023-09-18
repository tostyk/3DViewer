using System.Windows;

namespace _3DViewer.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly MainVM _mainVM;

        public MainWindow()
        {
            InitializeComponent();
            _mainVM = (MainVM)DataContext;
        }

        private void Image_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _mainVM.MouseDownCommand.Execute(e.GetPosition(this));
        }

        private void Image_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _mainVM.MouseMoveCommand.Execute(e.GetPosition(this));
        }

        private void Image_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _mainVM.MouseUpCommand.Execute(null);
        }

        private void Image_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            _mainVM.MouseWheelCommand.Execute(e.Delta);
        }
    }
}
