using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace MicMuter.WPF
{
    public class MainWindowViewModel : ReactiveObject, IActivatableViewModel
    {
        private Icon? _statusIcon;
        public Icon? StatusIcon
        {
            get => _statusIcon;
            set => this.RaiseAndSetIfChanged(ref _statusIcon, value);
        }

        public ViewModelActivator Activator { get; } = new();

        public MainWindowViewModel()
        {
            StatusIcon = Resources.RedMicrophone;

            this.WhenActivated(disposable =>
            {
                Disposable.Create(() => { /* cleanup */ }).DisposeWith(disposable);
            });
        }

    }
}
