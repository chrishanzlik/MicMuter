using Hardcodet.Wpf.TaskbarNotification;
using MicMuter.WPF.Helpers;
using MicMuter.WPF.Models;
using MicMuter.WPF.Services;
using ReactiveUI;
using Splat;
using System;
using System.Reactive.Disposables;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;

namespace MicMuter.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        readonly ViewModelActivator rootActivator;
        TaskbarIcon? taskbarIcon;

        public App()
        {
            rootActivator = new ViewModelActivator();

            Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetExecutingAssembly());
            Locator.CurrentMutable.RegisterConstant(rootActivator);
            Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetCallingAssembly());
            Locator.CurrentMutable.Register<IAudioService, AudioService>();
            Locator.CurrentMutable.Register<IDeviceInteractionService>(() =>
                new DeviceInteractionService(Locator.Current.GetService<ISerialPortWatcher>()));
            Locator.CurrentMutable.Register<ISerialPortWatcher, SerialPortWatcher>();

            RegisterInteractions();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            taskbarIcon = (TaskbarIcon)FindResource("TaskBarIcon");

            rootActivator.Activate();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            rootActivator.Deactivate();

            taskbarIcon?.Dispose();

            base.OnExit(e);
        }

        public int GetAffinityForView(Type view)
        {
            throw new NotImplementedException();
        }

        private static void RegisterInteractions()
        {
            Interactions.ConnectionErrorRetryInteraction.RegisterHandler(interaction =>
            {
                const string connectionErrorCaption = "MicMuter Verbindung Fehlgeschlagen";
                const string connectionErrorText = "Das MicMuter Gerät wurde nicht gefunden. Bitte stecken Sie das Gerät ein und verbinden es über das Taskleistenicon erneut.\r\n\r\nAlternativ können Sie jetzt einen neuen Verbindungsversuch vornehmen?";
                var result = MessageBox.Show(connectionErrorText, connectionErrorCaption, MessageBoxButton.YesNoCancel, MessageBoxImage.Error);
                interaction.SetOutput(result == MessageBoxResult.Yes ? ErrorRecoveryOption.Retry : ErrorRecoveryOption.Abort);
                return Task.CompletedTask;
            });
        }
    }
}
