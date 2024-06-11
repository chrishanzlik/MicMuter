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
        private readonly IDeviceInteractionService _deviceService;
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

        readonly ObservableAsPropertyHelper<bool> _initialConnected;
        public bool InitialConnected => _initialConnected.Value;

        public ViewModelActivator Activator { get; } = new();

        public Interaction<Unit, Unit> ConnectionErrorInteraction { get; }

        public ReactiveCommand<MicState, Unit> SendMicrophoneState { get; }
        public ReactiveCommand<bool, MicState> ToggleMicrophone { get; }
        public ReactiveCommand<Unit, Unit> Connect { get; }

        public MainWindowViewModel(
            IDeviceInteractionService? deviceInteractionService = null,
            IAudioService? audioService = null)
        {
            _deviceService = deviceInteractionService ?? Locator.Current.GetService<IDeviceInteractionService>()!;
            _audioService = audioService ?? Locator.Current.GetService<IAudioService>()!;

            SendMicrophoneState = ReactiveCommand.Create<MicState>(_deviceService.SendMicrophoneState);
            ToggleMicrophone = ReactiveCommand.CreateFromTask<bool, MicState>(_audioService.ToggleMicrophoneAsync);
            Connect = ReactiveCommand.CreateFromObservable(_deviceService.Connect);

            ConnectionErrorInteraction = new Interaction<Unit, Unit>();

            _initialConnected = Observable.Merge(
                Connect.Select(_ => true),
                Connect.ThrownExceptions.Select(_ => false)).ToProperty(this, x => x.InitialConnected);
            _micState = _audioService.StateChanges.ToProperty(this, x => x.MicState);
            _statusIcon = this.WhenAnyValue(x => x.MicState)
                .Select(state => state == MicState.Muted ? Resources.RedMicrophone : Resources.GreenMicrophone)
                .ToProperty(this, x => x.StatusIcon);

            this.WhenActivated(disposable =>
            {
                _audioService.StateChanges
                    .Merge(Observable.Interval(TimeSpan.FromSeconds(5)).Select(_ => _audioService.State))
                    .InvokeCommand(SendMicrophoneState)
                    .DisposeWith(disposable);

                _deviceService.ButtonPress
                    .WithLatestFrom(this.WhenAnyValue(x => x.PlaySounds), (value, playSounds) => playSounds)
                    .InvokeCommand(ToggleMicrophone)
                    .DisposeWith(disposable);

                Connect.Execute().Subscribe((_) => { }, error => {
                    ConnectionErrorInteraction.Handle(Unit.Default).Subscribe();
                });
            });
        }
    }
}
