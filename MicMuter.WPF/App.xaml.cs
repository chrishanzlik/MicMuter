using Hardcodet.Wpf.TaskbarNotification;
using MicMuter.WPF.Helpers;
using MicMuter.WPF.Models;
using MicMuter.WPF.Services;
using ReactiveUI;
using Splat;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Resx = MicMuter.WPF.Resources;

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

        private void RegisterInteractions()
        {
            Interactions.ConnectionError.RegisterHandler(interaction =>
            {
                var result = MessageBox.Show(Resx.ConnectionErrorText, Resx.ConnectionErrorCaption, MessageBoxButton.YesNoCancel, MessageBoxImage.Error);
                interaction.SetOutput(result == MessageBoxResult.Yes ? ErrorRecoveryOption.Retry : ErrorRecoveryOption.Abort);
                return Task.CompletedTask;
            });
        }
    }
}
