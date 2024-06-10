

using MicMuter.WPF.Models;
using System;
using System.Reactive;

namespace MicMuter.WPF.Services
{
    public interface IDeviceInteractionService
    {
        bool IsConnected { get; }

        IObservable<Unit> ButtonPress { get; }

        void Connect(string comPort);

        void SendMicrophoneState(MicState state);
    }
}
