using MicMuter.WPF.Helpers;
using MicMuter.WPF.Services;
using ReactiveUI;
using Splat;
using System.Reflection;
using System.Windows;

namespace MicMuter.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetCallingAssembly());
            Locator.CurrentMutable.Register<IAudioService, AudioService>();
            Locator.CurrentMutable.Register<IDeviceInteractionService>(() =>
                new DeviceInteractionService(Locator.Current.GetService<ISerialPortWatcher>()));
            Locator.CurrentMutable.Register<ISerialPortWatcher, SerialPortWatcher>();
        }
    }
}
