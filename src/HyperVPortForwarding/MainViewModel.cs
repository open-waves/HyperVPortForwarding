using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
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
            VmIpAddress = "192.168.137.4";
        }

        public ObservableCollection<ForwardedPort> Items { get; set; }

        public string HostPort { get; set; }
        public string VmPort { get; set; }
        
        private string _vmIpAddress;
        public string VmIpAddress
        {
            get { return _vmIpAddress; }
            set
            {
                SetField(ref _vmIpAddress, value, () => VmIpAddress);
            }
        }

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
                return _addPortCommand ?? (_addPortCommand = new RelayCommand(param => OnAddPortCommand(), param =>
                {
                    IPAddress ipAddress;
                    return string.IsNullOrWhiteSpace(HostPort) == false 
                        && IPAddress.TryParse(VmIpAddress, out ipAddress);
                }));
            }
        }

        private void OnAddPortCommand()
        {
            RunReadProcess(String.Format("netsh interface portproxy add v4tov4 {0} {1} {2}", HostPort, VmIpAddress, VmPort));

            RefreshConnections();
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
                return _removePortCommand ?? (_removePortCommand = new RelayCommand(param => OnRemovePortCommand(), param => SelectedForwardedPort != null));
            }
        }

        private void OnRemovePortCommand()
        {
            RunReadProcess("netsh interface portproxy delete v4tov4 " + SelectedForwardedPort.HostPort);
            RefreshConnections();
        }

        
        RelayCommand _removeAllPortsCommand;
        public ICommand RemoveAllPortsCommand
        {
            get
            {
                return _removeAllPortsCommand ?? (_removeAllPortsCommand = new RelayCommand(param => OnRemoveAllPortsCommand(), param => Items.Any()));
            }
        }

        private void OnRemoveAllPortsCommand()
        {
            foreach (var forwardedPort in Items)
            {
                RunReadProcess("netsh interface portproxy delete v4tov4 " + forwardedPort.HostPort);
            }

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