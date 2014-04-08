using System;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using log4net;
using PropertyChanged;

namespace MakingWaves.Tools.HyperVPortForwarding
{
    [ImplementPropertyChanged]
    public class NetworkConnectionViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string VmswitchName = "MyInternalSwitch";

        public bool VmswitchExists { get; set; }

        public NetworkConnectionViewModel()
        {
            VmswitchExists = IsNetworkConnectionLive();
            TemporaryAllowRunningPowerShellScripts();
            RegisterIcsLibrary();
            CheckScriptsFolder();
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

        [Conditional("Debug")]
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

        [Conditional("Debug")]
        private static void LogAndShutdown(CommandNotFoundException exception)
        {
            Log.Error("IsNetworkConnectionLive", exception);
            App.ShowErrorMessage("It looks like you have not installed HyperV yet.");
            Application.Current.Shutdown();
        }

        private void OnAddNetworkConnectionCommand()
        {
            ExecuteScript("addConnection");

            VmswitchExists = IsNetworkConnectionLive();

            ExecuteScript("setIp");
            ExecuteScript("selectVmswitchForVM");
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
            OnRemoveVmswitchFromVmCommand();
            ExecuteScript("disableSharing");

            ExecuteScript("removeConnection");

            VmswitchExists = IsNetworkConnectionLive();
        }

        private void ExecuteScript(string scriptName)
        {
            try
            {
                var powerShell = PowerShell.Create();

                powerShell.AddScript(GetScript(scriptName));

                powerShell.Invoke();
                PSDataCollection<ErrorRecord> error = powerShell.Streams.Error;
                LogIfErrorOccurred(error);
            }
            catch (Exception exception)
            {
                Log.Error("ExecuteScript", exception);
                App.ShowExceptionInMessageBox(exception);
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
                return _shareThroughEthernetCommand ?? (_shareThroughEthernetCommand = new RelayCommand(param => OnShareThroughEthernetCommand(), param => VmswitchExists));
            }
        }

        private void OnShareThroughEthernetCommand()
        {
            ExecuteScript("disableSharing");
            ExecuteScript("shareThroughEthernet");
        }

        RelayCommand _shareThroughWifiCommand;
        public ICommand ShareThroughWifiCommand
        {
            get
            {
                return _shareThroughWifiCommand ?? (_shareThroughWifiCommand = new RelayCommand(param => OnShareThroughWifiCommand(), param => VmswitchExists));
            }
        }

        private void OnShareThroughWifiCommand()
        {
            ExecuteScript("disableSharing");
            ExecuteScript("shareThroughWifi");
        }

        RelayCommand _clearSharingCommand;
        public ICommand ClearSharingCommand
        {
            get
            {
                return _clearSharingCommand ?? (_clearSharingCommand = new RelayCommand(param => OnClearSharingCommand(), param => VmswitchExists));
            }
        }

        private void OnClearSharingCommand()
        {
            ExecuteScript("disableSharing");
        }

        private void OnRemoveVmswitchFromVmCommand()
        {
            ExecuteScript("removeVmswitchFromVm");
        }

        private string GetScript(string name)
        {
            return string.Format(@"./Scripts/{0}.ps1", name);
        }
    }
}