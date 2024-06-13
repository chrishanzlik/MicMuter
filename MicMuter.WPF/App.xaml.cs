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
        readonly AutoSuspendHelper _autoSuspendHelper;
        readonly ViewModelActivator _rootActivator;
        TaskbarIcon? _taskbarIcon;

        public App()
        {
            _rootActivator = new ViewModelActivator();
            _autoSuspendHelper = new AutoSuspendHelper(this);
            RxApp.SuspensionHost.CreateNewAppState = () => new SystemTrayViewModel();
            RxApp.SuspensionHost.SetupDefaultSuspendResume(new AkavacheSuspensionDriver<SystemTrayViewModel>());

            Locator.CurrentMutable.RegisterConstant(_rootActivator);
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

            _taskbarIcon = (TaskbarIcon)FindResource("TaskBarIcon");
            _taskbarIcon.DataContext = RxApp.SuspensionHost.GetAppState<SystemTrayViewModel>();
            _taskbarIcon.Icon = Resx.SandGlass;

            _rootActivator.Activate();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _rootActivator.Deactivate();

            _taskbarIcon?.Dispose();

            base.OnExit(e);
        }

        private static void RegisterInteractions()
        {
            Interactions.ConnectionError.RegisterHandler(interaction =>
            {
                var result = MessageBox.Show(
                    Resx.ConnectionErrorText,
                    Resx.ConnectionErrorCaption,
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Error);

                interaction.SetOutput(result == MessageBoxResult.Yes
                    ? ErrorRecoveryOption.Retry
                    : ErrorRecoveryOption.Abort);
                
                return Task.CompletedTask;
            });
        }
    }
}
