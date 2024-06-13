using MicMuter.WPF.Helpers;
using MicMuter.WPF.Models;
using MicMuter.WPF.Services;
using ReactiveUI;
using Splat;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Windows;

namespace MicMuter.WPF
{
    [DataContract]
    public class SystemTrayViewModel : ReactiveObject, IActivatableViewModel
    {
        private readonly IAudioService _audioService;
        private readonly IDeviceInteractionService _deviceService;

        private bool _playSounds = true;
        [DataMember]
        public bool PlaySounds
        {
            get => _playSounds;
            set => this.RaiseAndSetIfChanged(ref _playSounds, value);
        }

        private bool _enableConnectionErrorPrompt = true;
        [DataMember]
        public bool ConnectionErrorPromptActive
        {
            get => _enableConnectionErrorPrompt;
            set => this.RaiseAndSetIfChanged(ref _enableConnectionErrorPrompt, value);
        }

        readonly ObservableAsPropertyHelper<string> _statusIconPath;
        public string StatusIconPath => _statusIconPath.Value;

        readonly ObservableAsPropertyHelper<MicState> _micState;
        public MicState MicState => _micState.Value;

        readonly ObservableAsPropertyHelper<bool> _initialConnected;
        public bool InitialConnected => _initialConnected.Value;

        public ReactiveCommand<MicState, Unit> SendMicrophoneState { get; }
        public ReactiveCommand<bool, MicState> ToggleMicrophone { get; }
        public ReactiveCommand<Unit, Unit> ToggleStatusSoundOutput { get; }
        public ReactiveCommand<Unit, Unit> ToggleConnectionErrorPromptActivation { get; }
        public ReactiveCommand<Unit, Unit> Connect { get; }
        public ReactiveCommand<Unit, Unit> ExitApplication { get; } = ReactiveCommand.Create(() => Application.Current.Shutdown());

        public ViewModelActivator Activator { get; }

        public SystemTrayViewModel()
        {
            Activator = Locator.Current.GetService<ViewModelActivator>() ?? throw new ArgumentException("No root activator provided.");
            _deviceService = Locator.Current.GetService<IDeviceInteractionService>()!;
            _audioService = Locator.Current.GetService<IAudioService>()!;

            SendMicrophoneState = ReactiveCommand.Create<MicState>(_deviceService.SendMicrophoneState);
            ToggleMicrophone = ReactiveCommand.CreateFromTask<bool, MicState>(_audioService.ToggleMicrophoneAsync);
            Connect = ReactiveCommand.CreateFromObservable(_deviceService.Connect);
            ToggleStatusSoundOutput = ReactiveCommand.Create(() => { });
            ToggleConnectionErrorPromptActivation = ReactiveCommand.Create(() => { });

            _initialConnected = Observable.Merge(
                Connect.Select(_ => true),
                Connect.ThrownExceptions.Select(_ => false)).ToProperty(this, x => x.InitialConnected);

            _micState = _audioService.StateChanges.ToProperty(this, x => x.MicState);

            _statusIconPath = this.WhenAnyValue(x => x.MicState)
                .Select(state => state == MicState.Muted ? "Resources/mic-red.ico" : "Resources/mic-green.ico")
                .ToProperty(this, x => x.StatusIconPath);

            this.WhenActivated(disposable =>
            {
                ToggleStatusSoundOutput
                    .Select((_) => !PlaySounds)
                    .BindTo(this, x => x.PlaySounds, () => true)
                    .DisposeWith(disposable);

                ToggleConnectionErrorPromptActivation
                    .Select((_) => !ConnectionErrorPromptActive)
                    .BindTo(this, x => x.ConnectionErrorPromptActive)
                    .DisposeWith(disposable);

                _audioService.StateChanges
                    .Merge(Observable.Interval(TimeSpan.FromSeconds(5)).Select(_ => _audioService.State))
                    .InvokeCommand(SendMicrophoneState)
                    .DisposeWith(disposable);

                _deviceService.ButtonPress
                    .WithLatestFrom(this.WhenAnyValue(x => x.PlaySounds), (value, playSounds) => playSounds)
                    .InvokeCommand(ToggleMicrophone)
                    .DisposeWith(disposable);

                TryToConnect().DisposeWith(disposable);
            });
        }

        private IDisposable TryToConnect()
        {
            return Connect.Execute().Subscribe((_) => { }, error => {
                if (ConnectionErrorPromptActive)
                {
                    Interactions.ConnectionError.Handle(error).Subscribe(recovery =>
                    {
                        if (recovery == ErrorRecoveryOption.Retry) TryToConnect();
                    });
                }
            });
        }
    }
}
