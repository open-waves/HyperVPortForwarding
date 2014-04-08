using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace MakingWaves.Tools.HyperVManagerHelper
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void PreviewTextInputForIpAddress(object sender, TextCompositionEventArgs e)
        {
            var regex = new Regex("[^0-9.]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/open-waves/HyperVPortForwarding");
        }
    }
}
