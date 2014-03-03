using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MakingWaves.Tools.HyperVPortForwarding
{
    public partial class MainWindow
    {
        private const string LocalPort = "Local port";
        private const string VmAddress = "VM address";
        private const string VmPort = "VM port";

        private ObservableCollection<String> _items;
        public ObservableCollection<String> Items
        {
            get { return _items ?? (_items = new ObservableCollection<String>()); }
        }

        public MainWindow()
        {
            InitializeComponent();
            PortsList.ItemsSource = Items;

            RefreshConnections(null, null);
        }

        private void RefreshConnections(object sender, RoutedEventArgs e)
        {
            if (sender != null && e != null)
                ClearInputs();

            _items.Clear();
            RunProcess("netsh interface portproxy show all");
        }

        private void Delete_OnClick(object sender, RoutedEventArgs e)
        {
            ClearInputs();
            if (PortsList.SelectedItem == null)
            {
                ValidationLabel.Content = "Please select active forwarding porto to delete";
                ValidationLabel.Visibility = Visibility.Visible;
            }
            else
            {
                ValidationLabel.Visibility = Visibility.Visible;
                ValidationLabel.Content = "Possible outcome:\n" +
                                          "Blank window - deletion of port forwarding completed successfully\n" +
                                          "'The system cannot find the file specified.' - no existing forwarding\n" +
                                          "'The requested operation requires elevation (Run as administrator)' - run program\nas administrator";
                var splitted = PortsList.SelectedValue.ToString().Split(',')[0].Split(' ');
                var deletedListenPort = splitted[splitted.Count() - 1];
                if (!String.IsNullOrEmpty(deletedListenPort))
                {
                    RunReadProcess("netsh interface portproxy delete v4tov4 " + deletedListenPort);
                    RefreshConnections(null, null);
                }
                else
                {
                    ValidationLabel.Content = LocalPort + " is empty!";
                }
            }

        }

        private void Add_OnClick(object sender, RoutedEventArgs e)
        {
            ClearInputs();
            if (String.IsNullOrEmpty(AddListenPort.Text) || String.IsNullOrEmpty(AddConnectAddress.Text) ||
                String.IsNullOrEmpty(AddConnectPort.Text) || int.Parse(AddListenPort.Text) == 0 || int.Parse(AddConnectPort.Text) == 0)
            {
                ValidationLabel.Content = String.Format("Please set {0}, {1} and {2}", LocalPort, VmAddress, VmPort);
                ValidationLabel.Visibility = Visibility.Visible;
                SelectErrorTextBox(AddConnectAddress);
                SelectErrorTextBox(AddListenPort);
                SelectErrorTextBox(AddConnectPort);
            }
            else
            {
                ValidationLabel.Visibility = Visibility.Visible;
                ValidationLabel.Content = "Desire outcome:\n" +
                                          "Blank window - port forwarding completed successfully\n" +
                                          "'The requested operation requires elevation (Run as administrator)' - run program\nas administrator";
                RunReadProcess(String.Format("netsh interface portproxy add v4tov4 {0} {1} {2}", AddListenPort.Text, AddConnectAddress.Text, AddConnectPort.Text));
                RefreshConnections(null, null);
            }
        }

        private void TextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = (TextBox)sender;
            if (tb == null)
                return;
            int i;
            if (!int.TryParse(tb.Text, out i))
                tb.Text = i.ToString(CultureInfo.InvariantCulture);
        }

        private void SelectErrorTextBox(TextBox textBox)
        {
            textBox.BorderThickness = new Thickness(3.0);
            textBox.BorderBrush = Brushes.Red;
        }

        private void ClearInputs()
        {
            ValidationLabel.Visibility = Visibility.Hidden;
            AddConnectAddress.BorderThickness = new Thickness(1.0);
            AddConnectAddress.BorderBrush = Brushes.DarkGray;
            AddListenPort.BorderThickness = new Thickness(1.0);
            AddListenPort.BorderBrush = Brushes.DarkGray;
            AddConnectPort.BorderThickness = new Thickness(1.0);
            AddConnectPort.BorderBrush = Brushes.DarkGray;
        }

        private void RunProcess(string args)
        {
            var proc = CreateProcess(args);
            proc.OutputDataReceived += process_OutputDataReceived;
            proc.Start();
            proc.BeginOutputReadLine();
            proc.WaitForExit();
        }
        private void RunReadProcess(string args)
        {
            var proc = CreateProcess(args);
            proc.Start();

            var outputReader = proc.StandardOutput;
            proc.WaitForExit();

            string displayText = "Output" + Environment.NewLine + "==============" + Environment.NewLine;
            displayText += outputReader.ReadToEnd();
            ConsoleBox.Text += displayText;
        }

        private static Process CreateProcess(string args)
        {
            var processStartInfo = ProcessStartInfo(args);
            var proc = new Process();
            proc.StartInfo = processStartInfo;
            return proc;
        }

        private static ProcessStartInfo ProcessStartInfo(string args)
        {
            var processStartInfo = new ProcessStartInfo("cmd.exe")
            {
                Arguments = "/c " + args
            };
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;
            return processStartInfo;
        }


        private void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (String.IsNullOrEmpty(e.Data)) return;
            if (e.Data.StartsWith("*"))
            {
                var strs = e.Data.Split(' ').Where(s => !String.IsNullOrEmpty(s) && s != "*").ToArray();
                var str = String.Format("{0}: {1}, {2}: {3}, {4}: {5}", LocalPort, strs[0], VmAddress, strs[1], VmPort,
                    strs[2]);

                Dispatcher.BeginInvoke(new Action(() => _items.Add(str)));
            }
        }

    }
}
