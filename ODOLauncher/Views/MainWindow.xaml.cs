using System.ComponentModel;
using ODOLauncher.ViewModels;

namespace ODOLauncher.Views
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            var vm = new MainViewModel();
            vm.Initialize(this);
            Closing += delegate(object sender, CancelEventArgs args) { vm.OnClosing(sender, args); };
            DataContext = vm;
        }
    }
}