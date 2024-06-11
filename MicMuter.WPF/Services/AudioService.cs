using MicMuter.WPF.Models;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace MicMuter.WPF.Services
{
    public class AudioService : IAudioService, IDisposable
    {
        readonly MMDeviceEnumerator _enumerator = new();

        AudioEndpointVolume DefaultAudioEndpointVolume =>
            _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications).AudioEndpointVolume;

        public MicState State {
            get => DefaultAudioEndpointVolume.Mute
                ? MicState.Muted
                : MicState.Activated;
        }

        public IObservable<MicState> StateChanges => Observable.Interval(TimeSpan.FromMilliseconds(500))
            .Select(_ => State)
            .DistinctUntilChanged()
            .Publish()
            .RefCount();

        public async Task MuteMicrophoneAsync(bool playSound)
        {
            DefaultAudioEndpointVolume.Mute = true;

            if (playSound)
            {
                await PlayMicrophoneMutedSoundAsync();
            }
        }

        public async Task UnmuteMicrophoneAsync(bool playSound)
        {
            DefaultAudioEndpointVolume.Mute = false;

            if (playSound)
            {
                await PlayMicrophoneActivatedSoundAsync();
            }
        }

        public async Task<MicState> ToggleMicrophoneAsync(bool playSound)
        {
            if (State == MicState.Muted)
            {
                await UnmuteMicrophoneAsync(playSound);
                return MicState.Activated;
            }
            else
            {
                await MuteMicrophoneAsync(playSound);
                return MicState.Muted;
            }
        }

        private static Task PlayMicrophoneMutedSoundAsync() => PlayMp3FileAsync(Resources.MicrophoneMutedSound);

        private static Task PlayMicrophoneActivatedSoundAsync() => PlayMp3FileAsync(Resources.MicrophoneActivatedSound);

        private static async Task PlayMp3FileAsync(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            stream.Seek(0, SeekOrigin.Begin);

            using var reader = new Mp3FileReader(stream);
            using var waveOut = new WaveOut();
            waveOut.Init(reader);
            waveOut.Play();

            while (waveOut.PlaybackState == PlaybackState.Playing)
            {
                await Task.Delay(100);
            }
        }

        public void Dispose() => _enumerator.Dispose();
    }
}
