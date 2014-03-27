using System;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using log4net;

namespace MakingWaves.Tools.HyperVPortForwarding
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected override void OnStartup(StartupEventArgs e)
        {
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;
            log4net.Config.XmlConfigurator.Configure();
            base.OnStartup(e);
        }

        void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error("Dispatcher_UnhandledException", e.Exception);
            ShowExceptionInMessageBox(e.Exception);
            e.Handled = true;
        }

        public static void ShowExceptionInMessageBox(Exception exception)
        {
            string errorMessage = string.Format("An unhandled exception occurred: {0}", exception.Message);
            MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static void ShowMessageBox(string messages)
        {
            MessageBox.Show(messages, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
