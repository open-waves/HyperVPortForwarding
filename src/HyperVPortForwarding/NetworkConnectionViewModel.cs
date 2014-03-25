using System;
using System.Diagnostics;
using System.Management.Automation;
using System.Reflection;
using System.Windows.Input;
using log4net;

namespace MakingWaves.Tools.HyperVPortForwarding
{
    public class NetworkConnectionViewModel : ViewModelBase
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string VmswitchName = "HyperVVmswitch";

        public NetworkConnectionViewModel()
        {
            VmswitchExists = IsNetworkConnectionLive();
            TemporaryAllowRunningPowerShellScripts();
            RegisterLibrary();
        }

        private void TemporaryAllowRunningPowerShellScripts()
        {
            var powerShell = PowerShell.Create();

            powerShell.AddScript("Set-ExecutionPolicy Bypass -Scope Process");

            powerShell.Invoke();
            PSDataCollection<ErrorRecord> error = powerShell.Streams.Error;
            LogIfErrorOccurred(error);
        }

        private void RegisterLibrary()
        {
            // TODO This needs to be done only once, but don't see good way to check it (now)
            Process process = MainViewModel.CreateProcess("regsvr32 hnetcfg.dll");
            process.Start();
        }

        private bool _vmswitchExists;
        public bool VmswitchExists
        {
            get { return _vmswitchExists; }
            set
            {
                SetField(ref _vmswitchExists, value, () => VmswitchExists);
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
            var powerShell = PowerShell.Create();

            powerShell.AddCommand("Get-vmswitch");

            var wmswitches = powerShell.Invoke();
            foreach (var wmswitch in wmswitches)
            {
                var psPropertyInfo= wmswitch.Properties["Name"];
                var name = psPropertyInfo.Value as string;
                if (name == VmswitchName)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnAddNetworkConnectionCommand()
        {
            ExecuteScript("addConnection");

            VmswitchExists = IsNetworkConnectionLive();
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
                throw;
            }
        }

        private void LogIfErrorOccurred(PSDataCollection<ErrorRecord> errors)
        {
            foreach (var errorRecord in errors)
            {
                Log.Warn("PowerShell error", errorRecord.Exception);
            }
        }

        RelayCommand _setIpCommand;
        public ICommand SetIpCommand
        {
            get
            {
                return _setIpCommand ?? (_setIpCommand = new RelayCommand(param => OnSetIpCommand(), param => true));
            }
        }

        private void OnSetIpCommand()
        {
            ExecuteScript("setIp");
        }

        RelayCommand _selectVmswitchForVmCommand;
        public ICommand SelectVmswitchForVmCommand
        {
            get
            {
                return _selectVmswitchForVmCommand ?? (_selectVmswitchForVmCommand = new RelayCommand(param => OnSelectVmswitchForVmCommand(), param => true));
            }
        }

        private void OnSelectVmswitchForVmCommand()
        {
            ExecuteScript("selectVmswitchForVM");
        }

        RelayCommand _shareThroughEthernetCommand;
        public ICommand ShareThroughEthernetCommand
        {
            get
            {
                return _shareThroughEthernetCommand ?? (_shareThroughEthernetCommand = new RelayCommand(param => OnShareThroughEthernetCommand(), param => true));
            }
        }

        private void OnShareThroughEthernetCommand()
        {
            ExecuteScript("disableEthernet");
//            ExecuteScript("selectVmswitchForVM");
            ExecuteScript("shareThroughEthernet");
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