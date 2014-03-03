using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;

namespace MakingWaves.Tools.HyperVPortForwarding
{
    public class MainViewModel : ViewModelBase
    {
        private readonly Dispatcher _dispatcher;

        public MainViewModel()
        {
            Items = new ObservableCollection<ForwardedPort>();

            _dispatcher = Dispatcher.CurrentDispatcher;
            RefreshConnections();
        }

        public ObservableCollection<ForwardedPort> Items { get; set; }

        private ForwardedPort _selectedForwardedPort;
        public ForwardedPort SelectedForwardedPort
        {
            get { return _selectedForwardedPort; }
            set { SetField(ref _selectedForwardedPort, value, () => SelectedForwardedPort); }
        }

        RelayCommand _addPortCommand;
        public ICommand AddPortCommand
        {
            get
            {
                return _addPortCommand ?? (_addPortCommand = new RelayCommand(param => OnAddPortCommand(), param => true));
            }
        }

        private void OnAddPortCommand()
        {
            // TODO validation
//                        if (String.IsNullOrEmpty(AddListenPort.Text) || String.IsNullOrEmpty(AddConnectAddress.Text) ||
//                            String.IsNullOrEmpty(AddConnectPort.Text) || int.Parse(AddListenPort.Text) == 0 || int.Parse(AddConnectPort.Text) == 0)
//                        {
//                            // validation
//                            return;
//                            ValidationLabel.Content = String.Format("Please set {0}, {1} and {2}", LocalPort, VmAddress, VmPort);
//                            ValidationLabel.Visibility = Visibility.Visible;
//                            SelectErrorTextBox(AddConnectAddress);
//                            SelectErrorTextBox(AddListenPort);
//                            SelectErrorTextBox(AddConnectPort);
//                        }
//                        else
            {
                // TODO todo
                RunReadProcess(String.Format("netsh interface portproxy add v4tov4 {0} {1} {2}", 8003, "192.168.2.2", 8003));

                RefreshConnections();
            }
        }

        public void RefreshConnections()
        {
            Items.Clear();
            RunProcess("netsh interface portproxy show all");
        }

        RelayCommand _removePortCommand;
        public ICommand RemovePortCommand
        {
            get
            {
                return _removePortCommand ?? (_removePortCommand = new RelayCommand(param => OnRemovePortCommand(), param => true));
            }
        }

        private void OnRemovePortCommand()
        {
            RunReadProcess("netsh interface portproxy delete v4tov4 " + SelectedForwardedPort.HostPort); // TODO not sure
            RefreshConnections();
        }

        private void RunProcess(string args)
        {
            var process = CreateProcess(args);
            process.OutputDataReceived += process_OutputDataReceived;
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
        }

        private void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(e.Data) || e.Data.StartsWith("*") == false)
                return;

            var forwardedPort = new ForwardedPortFactory().Create(e.Data);

            Action del = () => Items.Add(forwardedPort);
            _dispatcher.BeginInvoke(del);
        }

        private static void RunReadProcess(string args)
        {
            var process = CreateProcess(args);
            process.Start();
            process.WaitForExit();
        }

        private static Process CreateProcess(string args)
        {
            var processStartInfo = new ProcessStartInfo("cmd.exe")
            {
                Arguments = "/c " + args,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = new Process
            {
                StartInfo = processStartInfo
            };

            return process;
        }
    }
}