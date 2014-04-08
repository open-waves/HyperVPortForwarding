using System.Windows;
using System.Windows.Controls;

namespace MakingWaves.Tools.HyperVManagerHelper
{
    /// <summary>
    /// Interaction logic for NetworkConnectionUserControl.xaml
    /// </summary>
    public partial class NetworkConnectionUserControl : UserControl
    {
        private bool _loaded = false;

        public NetworkConnectionUserControl()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_loaded)
                return;
            
            var networkConnectionViewModel = DataContext as NetworkConnectionViewModel;
            networkConnectionViewModel.RefreshVmswitch();
            _loaded = true;
        }
    }
}
