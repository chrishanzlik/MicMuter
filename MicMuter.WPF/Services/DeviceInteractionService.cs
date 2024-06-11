using MicMuter.WPF.Helpers;
using MicMuter.WPF.Models;
using Splat;
using System;
using System.IO.Ports;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace MicMuter.WPF.Services
{
    public class DeviceInteractionService(ISerialPortWatcher? serialPortWatcher = null) : IDeviceInteractionService, IDisposable
    {
        private const byte MIC_OFF_BYTE = 0;
        private const byte MIC_ON_BYTE = 1;
        private const byte HANDSHAKE_BYTE = 6;

        private readonly ISerialPortWatcher _serialPortWatcher = serialPortWatcher ?? Locator.Current.GetService<ISerialPortWatcher>()!;
        private readonly BehaviorSubject<SerialPort?> _serialPortChange = new(null);

        public IObservable<Unit> ButtonPress => this._serialPortChange
            .AsObservable()
            .Where(x => x != null)
            .Select(x => ButtonPresses(x!))
            .Switch();

        public IObservable<Unit> Connect()
        {
            var identifications = SerialPort.GetPortNames()
                .Select(DeviceHandshakes)
                .Merge()
                .Where(x => x != null)
                .Publish()
                .RefCount();

            var syncSub = identifications.Subscribe(_serialPortChange.OnNext);

            var reconnectSub = _serialPortWatcher.DeviceConnected.Subscribe(ports =>
            {
                var port = _serialPortChange.Value;
                if (port != null && !port.IsOpen)
                {
                    try { port.Open(); }
                    catch (Exception) { }
                }
            });

            return Observable.Create<Unit>(observer =>
            {
                var initSub = identifications.Select(_ => Unit.Default).Subscribe(observer);

                identifications
                    .Take(1)
                    .Where(x => x == null)
                    .Timeout(TimeSpan.FromSeconds(5))
                    .Catch(Observable.Return<SerialPort?>(null))
                    .Subscribe((_) => observer.OnError(new Exception("No compatible device found.")));

                return () => {
                    syncSub.Dispose();
                    initSub.Dispose();
                    reconnectSub.Dispose();
                    this.Dispose();
                };
            });
        }

        public void SendMicrophoneState(MicState state)
        {
            var port = _serialPortChange.Value;

            if (port != null && port.IsOpen)
            {
                try
                {
                    port.Write([(byte)state], 0, 1);
                }
                catch (UnauthorizedAccessException) { }
            }
        }


        private static IObservable<SerialPort> DeviceHandshakes(string comPort) => Observable.Create<SerialPort>(observer =>
        {
            var sp = new SerialPort(comPort, 9600, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = 10000,
                WriteTimeout = 10000
            };

            sp.Open();

            // Seems weird, but required for reconnect... :/
            System.Threading.Thread.Sleep(4000);

            var sub = Observable.FromEventPattern<SerialDataReceivedEventHandler, SerialDataReceivedEventArgs>(
                    x => sp.DataReceived += x,
                    x => sp.DataReceived -= x)
                .Take(1)
                .Timeout(TimeSpan.FromSeconds(10))
                .Where(_ => (byte)sp.ReadByte() is HANDSHAKE_BYTE)
                .Select((ev) => (ev.Sender as SerialPort) ?? sp)
                .Subscribe(
                    (next) => observer.OnNext(next),
                    error =>
                    {
                        if (sp.IsOpen)
                        {
                            sp.Close();
                        }
                        sp.Dispose();
                    },
                    () => { });

            sp.Write([6], 0, 1);

            return () => sub.Dispose();
        });

        private static IObservable<Unit> ButtonPresses(SerialPort comPort) => Observable.Create<Unit>(observer =>
        {
            if (!comPort.IsOpen)
            {
                comPort.Open();
            }

            var sub = Observable.FromEventPattern<SerialDataReceivedEventHandler, SerialDataReceivedEventArgs>(
                    x => comPort.DataReceived += x,
                    x => comPort.DataReceived -= x)
                .Where(_ => (byte)comPort.ReadByte() is MIC_OFF_BYTE or MIC_ON_BYTE)
                .Select((_) => Unit.Default)
                .Subscribe(observer);

            return () => {
                sub.Dispose();
                comPort.Close();
                comPort.Dispose();
            };
        });

        public void Dispose()
        {
            var port = _serialPortChange.Value;

            if (port?.IsOpen ?? false)
            {
                port?.Close();
                port?.Dispose();
            }
        }
    }
}
