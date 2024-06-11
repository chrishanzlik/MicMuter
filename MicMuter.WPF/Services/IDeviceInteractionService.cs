

using MicMuter.WPF.Models;
using System;
using System.Reactive;

namespace MicMuter.WPF.Services
{
    public interface IDeviceInteractionService
    {
        IObservable<Unit> ButtonPress { get; }

        IObservable<Unit> Connect();

        void SendMicrophoneState(MicState state);
    }
}
