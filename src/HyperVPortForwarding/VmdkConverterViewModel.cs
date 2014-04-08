using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using PropertyChanged;
using Xcarab.Vmdk;
using Xcarab.VmdkConvert;

namespace MakingWaves.Tools.HyperVManagerHelper
{
    [ImplementPropertyChanged]
    public class VmdkConverterViewModel
    {
        public string SourceFileText { get; set; }
        public string TargetFileText { get; set; }
        public bool IsConverting { get; set; }
        public bool IsNotConverting { get { return IsConverting == false; } }
        public int ConvertedPercentageLevel { get; set; }
        public string VirtualMachineSize { get; set; }

        readonly VmdkConverter _vmdkConverter = new VmdkConverter();

        public VmdkConverterViewModel()
        {
            IsConverting = false;
        }

        RelayCommand _selectSourceFileCommand;
        public ICommand SelectSourceFileCommand
        {
            get
            {
                return _selectSourceFileCommand ?? (_selectSourceFileCommand = new RelayCommand(param => OnSelectSourceFileCommand(), param => true));
            }
        }

        private void OnSelectSourceFileCommand()
        {
            var openFileDialog = new OpenFileDialog
            {
                DefaultExt = ".vmdk",
                FileName = SourceFileText,
                CheckFileExists = true
            };

            bool? result = openFileDialog.ShowDialog();

            if (result == true)
            {
                try
                {
                    DiskInfo diskInfo = new DiskInfo();
                    VmdkFile.OpenVmdk(openFileDialog.FileName, diskInfo);
                    if ((int)diskInfo.Capacity == 0)
                    {
                        MessageBox.Show("Invalid Vmdk file, please select another");
                    }
                    else
                    {
                        SourceFileText = openFileDialog.FileName;
                        long size = diskInfo.Capacity * 512L / 1000000L;
                        VirtualMachineSize = string.Format("{0:# ### ###} MB", size);

                        VmdkFile.CloseVmdk(diskInfo);
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Invalid Vmdk file or file could not be opened");
                }
            }
        }

        RelayCommand _selectTargetFileCommand;
        public ICommand SelectTargetFileCommand
        {
            get
            {
                return _selectTargetFileCommand ?? (_selectTargetFileCommand = new RelayCommand(param => OnSelectTargetFileCommand(), param => true));
            }
        }

        private void OnSelectTargetFileCommand()
        {
            var openFileDialog = new OpenFileDialog
            {
                DefaultExt = ".vhd",
                FileName = TargetFileText,
                CheckFileExists = false
            };

            bool? result = openFileDialog.ShowDialog();

            if (result == true)
            {
                TargetFileText = openFileDialog.FileName;
            }
        }

        RelayCommand _convertCommand;
        public ICommand ConvertCommand
        {
            get
            {
                return _convertCommand ?? (_convertCommand = new RelayCommand(param => OnConvertCommand(), param => CanConvert()));
            }
        }

        private bool CanConvert()
        {
            if (string.IsNullOrWhiteSpace(SourceFileText))
                return false;

            if (string.IsNullOrWhiteSpace(TargetFileText))
                return false;

            var directoryName = Path.GetDirectoryName(TargetFileText);
            if (directoryName == null)
                return false;

            return (File.Exists(SourceFileText)) &&
                    Directory.Exists(directoryName)
                    && IsNotConverting;
        }

        private void OnConvertCommand()
        {
            Application.Current.Dispatcher.Invoke((Action)(() => IsConverting = true));

            BackgroundWorker worker = new BackgroundWorker();

            worker.DoWork += delegate
            {
                ConvertAsync();
            };

            worker.RunWorkerAsync();
        }


        private void ConvertAsync()
        {

            ConverterDelegate converterDelegate = _vmdkConverter.Convert;
            IAsyncResult result = converterDelegate.BeginInvoke(SourceFileText, TargetFileText, null, null);

            while (!result.AsyncWaitHandle.WaitOne(100, false))
            {
                if (_vmdkConverter.Aborted)
                    break;

                var level = (((double)_vmdkConverter.ConvertedSectors) / _vmdkConverter.TotalSectors) * 100;

                Application.Current.Dispatcher.Invoke((Action)(() => ConvertedPercentageLevel = (int)level));
            }

            converterDelegate.EndInvoke(result);

            Application.Current.Dispatcher.Invoke((Action)(() => IsConverting = false));

            int statusLevel = _vmdkConverter.Aborted ? 0 : 100;

            Application.Current.Dispatcher.Invoke((Action)(() => ConvertedPercentageLevel = statusLevel));

            if (!_vmdkConverter.Aborted)
            {
                MessageBox.Show("Conversion complete.", "Conversion complete");
            }
        }

        RelayCommand _abortCommand;
        public ICommand AbortCommand
        {
            get
            {
                return _abortCommand ?? (_abortCommand = new RelayCommand(param => _vmdkConverter.CancelConvert(), param => IsConverting));
            }
        }
    }
}