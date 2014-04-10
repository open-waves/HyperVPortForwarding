using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Input;
using log4net;
using MakingWaves.Tools.HyperVManagerHelper.PowerShellParsing;
using PropertyChanged;

namespace MakingWaves.Tools.HyperVManagerHelper
{
    [ImplementPropertyChanged]
    public class NetworkConnectionViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string VmswitchName = "MyInternalSwitch";

        public bool VmswitchExists { get; set; }
        public bool IsBusy { get; set; }
        public bool IsSharedViaEthernet { get; set; }
        public bool IsSharedViaWifi { get; set; }

        public NetworkConnectionViewModel()
        {
            TemporaryAllowRunningPowerShellScripts();
            RegisterIcsLibrary();
            CheckScriptsFolder();
        }

        public void RefreshVmswitch()
        {
            RunAsyncWithBusyIndicator(() =>
            {
                var isNetworkConnectionLive = IsNetworkConnectionLive();
                VmswitchExists = isNetworkConnectionLive;

                if (isNetworkConnectionLive)
                {
                    RefreshConnectionSharing();
                }
            });
        }

        private void RunAsyncWithBusyIndicator(Action action)
        {
            BackgroundWorker worker = new BackgroundWorker();

            worker.DoWork += (o, ea) => action();

            worker.RunWorkerCompleted += (o, ea) =>
            {
                MakeNotBusy();
                CommandManager.InvalidateRequerySuggested();
            };

            MakeBusy();
            worker.RunWorkerAsync();
        }

        private void MakeNotBusy()
        {
            Application.Current.Dispatcher.Invoke((Action)(() => IsBusy = false));
        }

        private void MakeBusy()
        {
            Application.Current.Dispatcher.Invoke((Action)(() => IsBusy = true));
        }

        private void TemporaryAllowRunningPowerShellScripts()
        {
            var powerShell = PowerShell.Create();

            powerShell.AddScript("Set-ExecutionPolicy Bypass -Scope Process");

            powerShell.Invoke();
            PSDataCollection<ErrorRecord> error = powerShell.Streams.Error;
            LogIfErrorOccurred(error);
        }

        private void RegisterIcsLibrary()
        {
            var dllName = "hnetcfg.dll";
            if (File.Exists(Path.Combine(Environment.SystemDirectory, dllName)))
                return;

            Process process = MainViewModel.CreateProcess(string.Format("regsvr32 {0}", dllName));
            process.Start();
        }

        [Conditional("RELEASE")]
        private void CheckScriptsFolder()
        {
            if (Directory.Exists("Scripts") == false)
            {
                App.ShowErrorMessage("'Scripts' folder is missing. You need to copy it.");
                Application.Current.Shutdown();
            }
        }

        RelayCommand _addNewNetworkConnectionCommand;
        public ICommand AddNetworkConnectionCommand
        {
            get
            {
                return _addNewNetworkConnectionCommand ?? (_addNewNetworkConnectionCommand = new RelayCommand(param => OnAddNetworkConnectionCommand(), param => VmswitchExists == false));
            }
        }

        private bool IsNetworkConnectionLive()
        {
            try
            {
                var powerShell = PowerShell.Create();

                powerShell.AddCommand("Get-vmswitch");

                var wmswitches = powerShell.Invoke();
                foreach (var wmswitch in wmswitches)
                {
                    var psPropertyInfo = wmswitch.Properties["Name"];
                    var name = psPropertyInfo.Value as string;
                    if (name == VmswitchName)
                    {
                        return true;
                    }
                }
            }
            catch (CommandNotFoundException exception)
            {
                LogAndShutdown(exception);
            }

            return false;
        }

        [Conditional("RELEASE")]
        private static void LogAndShutdown(CommandNotFoundException exception)
        {
            Log.Error("IsNetworkConnectionLive", exception);
            App.ShowErrorMessage("It looks like you have not installed Hyper-V yet.");
            Application.Current.Shutdown();
        }

        private void OnAddNetworkConnectionCommand()
        {
            RunAsyncWithBusyIndicator(() =>
            {
                ExecuteScript("addConnection");

                VmswitchExists = IsNetworkConnectionLive();

                ExecuteScript("setIp");
                ExecuteScript("selectVmswitchForVM");
            });
        }

        RelayCommand _removeNetworkConnectionCommand;
        public ICommand RemoveNetworkConnectionCommand
        {
            get
            {
                return _removeNetworkConnectionCommand ?? (_removeNetworkConnectionCommand = new RelayCommand(param => OnRemoveNetworkConnectionCommand(), param => VmswitchExists));
            }
        }

        private void OnRemoveNetworkConnectionCommand()
        {
            RunAsyncWithBusyIndicator(() =>
            {
                ExecuteScript("disableSharing");
                RefreshConnectionSharing();

                ExecuteScript("removeVmswitchFromVm");
                ExecuteScript("disableSharing");

                ExecuteScript("removeConnection");

                VmswitchExists = IsNetworkConnectionLive();
            });
        }

        private IEnumerable<PSObject> ExecuteScript(string scriptName)
        {
            try
            {
                var powerShell = PowerShell.Create();

                powerShell.AddScript(GetScript(scriptName));

                Collection<PSObject> results = powerShell.Invoke();
                PSDataCollection<ErrorRecord> error = powerShell.Streams.Error;
                LogIfErrorOccurred(error);

                return results;
            }
            catch (Exception exception)
            {
                Log.Error("ExecuteScript", exception);
                App.ShowExceptionInMessageBox(exception);

                return Enumerable.Empty<PSObject>();
            }
        }

        private void LogIfErrorOccurred(PSDataCollection<ErrorRecord> errors)
        {
            foreach (var errorRecord in errors)
            {
                Log.Warn("PowerShell error", errorRecord.Exception);
            }
        }

        RelayCommand _shareThroughEthernetCommand;
        public ICommand ShareThroughEthernetCommand
        {
            get
            {
                return _shareThroughEthernetCommand ?? (_shareThroughEthernetCommand = new RelayCommand(param => OnShareThroughEthernetCommand(), param => VmswitchExists && IsSharedViaEthernet == false));
            }
        }

        private void OnShareThroughEthernetCommand()
        {
            RunAsyncWithBusyIndicator(() =>
            {
                ExecuteScript("disableSharing");
                ExecuteScript("shareThroughEthernet");

                RefreshConnectionSharing();
            });
        }

        private void RefreshConnectionSharing()
        {
            IEnumerable<PSObject> results = ExecuteScript("refreshSharing");

            StringBuilder sb = new StringBuilder();
            foreach (var psObject in results)
            {
                sb.AppendLine(psObject.ToString());
            }

            SharingParser sharingParser = new SharingParser();
            SharingDto sharingDto = sharingParser.Parse(sb.ToString());

            Application.Current.Dispatcher.Invoke((Action)(() => IsSharedViaEthernet = sharingDto.IsEthernetShared));
            Application.Current.Dispatcher.Invoke((Action)(() => IsSharedViaWifi = sharingDto.IsWifiShared));
        }

        RelayCommand _shareThroughWifiCommand;
        public ICommand ShareThroughWifiCommand
        {
            get
            {
                return _shareThroughWifiCommand ?? (_shareThroughWifiCommand = new RelayCommand(param => OnShareThroughWifiCommand(), param => VmswitchExists && IsSharedViaWifi == false));
            }
        }

        private void OnShareThroughWifiCommand()
        {
            RunAsyncWithBusyIndicator(() =>
            {
                ExecuteScript("disableSharing");
                ExecuteScript("shareThroughWifi");

                RefreshConnectionSharing();
            });
        }

        RelayCommand _clearSharingCommand;
        public ICommand ClearSharingCommand
        {
            get
            {
                return _clearSharingCommand ?? (_clearSharingCommand = new RelayCommand(param => OnClearSharingCommand(), param => VmswitchExists && (IsSharedViaEthernet || IsSharedViaWifi)));
            }
        }

        private void OnClearSharingCommand()
        {
            RunAsyncWithBusyIndicator(() =>
            {
                ExecuteScript("disableSharing");

                RefreshConnectionSharing();
            });
        }

        private string GetScript(string name)
        {
            return string.Format(@"./Scripts/{0}.ps1", name);
        }
    }
}