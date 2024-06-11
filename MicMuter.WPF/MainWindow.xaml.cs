using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows;

namespace MicMuter.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        //TODO: resx
        const string connectionErrorCaption = "MicMuter Verbindung Fehlgeschlagen";
        const string connectionErrorText = "Das MicMuter Gerät wurde nicht gefunden. Bitte stecken Sie das Gerät ein und verbinden es über das Taskleistenicon erneut.\r\n\r\nAlternativ können Sie jetzt einen neuen Verbindungsversuch vornehmen?";

        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new MainWindowViewModel();

            this.WhenActivated(disposable =>
            {
                this.OneWayBind(ViewModel, vm => vm.StatusIcon, v => v.TaskbarIcon.Icon).DisposeWith(disposable);

                this.BindInteraction(ViewModel, x => x.ConnectionErrorInteraction, (context) =>
                {
                    var result = MessageBox.Show(connectionErrorText, connectionErrorCaption, MessageBoxButton.YesNoCancel, MessageBoxImage.Error);
                    context.SetOutput(result == MessageBoxResult.Yes);
                    return Task.CompletedTask;
                }).DisposeWith(disposable);
            });
        }
    }
}
