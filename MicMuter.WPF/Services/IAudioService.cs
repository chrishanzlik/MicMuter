using MicMuter.WPF.Models;
using System;
using System.Threading.Tasks;

namespace MicMuter.WPF.Services
{
    public interface IAudioService
    {
        MicState State { get; }

        IObservable<MicState> StateChanges { get; }

        Task MuteMicrophoneAsync(bool playSound);

        Task UnmuteMicrophoneAsync(bool playSound);

        Task<MicState> ToggleMicrophoneAsync(bool playSound);
    }
}
