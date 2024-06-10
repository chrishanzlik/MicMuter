using MicMuter.WPF.Models;
using MicMuter.WPF.Services;
using ReactiveUI;
using Splat;
using System;
using System.Drawing;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace MicMuter.WPF
{
    public class MainWindowViewModel : ReactiveObject, IActivatableViewModel
    {
        private readonly IDeviceInteractionService _deviceInteractionService;
        private readonly IAudioService _audioService;

        bool _playSounds = true;
        public bool PlaySounds
        {
            get => _playSounds;
            set => this.RaiseAndSetIfChanged(ref _playSounds, value);
        }

        readonly ObservableAsPropertyHelper<MicState> _micState;
        public MicState MicState => _micState.Value;

        readonly ObservableAsPropertyHelper<Icon> _statusIcon;
        public Icon StatusIcon => _statusIcon.Value;

        public ViewModelActivator Activator { get; } = new();

        public ReactiveCommand<MicState, Unit> SendMicrophoneState { get; }
        public ReactiveCommand<bool, MicState> ToggleMicrophone { get; }

        public MainWindowViewModel(
            IDeviceInteractionService? deviceInteractionService = null,
            IAudioService? audioService = null)
        {
            _deviceInteractionService = deviceInteractionService ?? Locator.Current.GetService<IDeviceInteractionService>()!;
            _audioService = audioService ?? Locator.Current.GetService<IAudioService>()!;

            SendMicrophoneState = ReactiveCommand.Create<MicState>(_deviceInteractionService.SendMicrophoneState);
            ToggleMicrophone = ReactiveCommand.CreateFromTask<bool, MicState>(_audioService.ToggleMicrophoneAsync);

            _micState = _audioService.StateChanges.ToProperty(this, x => x.MicState);

            _statusIcon = this.WhenAnyValue(x => x.MicState)
                .Select(state => state == MicState.Muted ? Resources.RedMicrophone : Resources.GreenMicrophone)
                .ToProperty(this, x => x.StatusIcon);

            this.WhenActivated(disposable =>
            {
                _audioService.StateChanges
                    .InvokeCommand(SendMicrophoneState)
                    .DisposeWith(disposable);

                _deviceInteractionService.ButtonPress
                    .CombineLatest(this.WhenAnyValue(x => x.PlaySounds))
                    .Select((x) => x.Second)
                    .InvokeCommand(ToggleMicrophone)
                    .DisposeWith(disposable);

                _deviceInteractionService.Connect("COM3");

                Disposable.Create(() => { }).DisposeWith(disposable);
            });
        }

    }
}
