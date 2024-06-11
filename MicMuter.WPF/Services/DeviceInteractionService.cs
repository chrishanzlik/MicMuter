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
    public class DeviceInteractionService : IDeviceInteractionService, IDisposable
    {
        private readonly ISerialPortWatcher _serialPortWatcher;
        private readonly BehaviorSubject<SerialPort?> _serialPortChange = new(null);


        public IObservable<Unit> ButtonPress => this._serialPortChange
            .AsObservable()
            .Where(x => x != null)
            .Select(x => ListenForButtonPresses(x!))
            .Switch();

        public DeviceInteractionService(ISerialPortWatcher? serialPortWatcher = null)
        {
            _serialPortWatcher = serialPortWatcher ?? Locator.Current.GetService<ISerialPortWatcher>()!;


            _serialPortWatcher.DeviceDisconnected.Subscribe(ports =>
            {
                var port = this._serialPortChange.Value;
                if (port != null && !port.IsOpen)
                {
                    port.Dispose();
                    GC.Collect();
                    _serialPortChange.OnNext(null);
                }
            });
        }

        public IObservable<Unit> Connect()
        {
            var identifications = SerialPort.GetPortNames()
                .Select(DeviceHandshake)
                .Merge()
                .Publish()
                .RefCount();

            var syncSub = identifications.Subscribe(_serialPortChange.OnNext);

            return Observable.Create<Unit>(observer =>
            {
                var initSub = identifications.Select(_ => Unit.Default).Subscribe(observer);

                return () => {
                    syncSub.Dispose();
                    initSub.Dispose();
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


        private IObservable<SerialPort> DeviceHandshake(string comPort)
        {
            return Observable.Create<SerialPort>(observer =>
            {
                var sp = new SerialPort(comPort, 9600, Parity.None, 8, StopBits.One);

                sp.Open();

                // Seems weird, but required for reconnect... :/
                System.Threading.Thread.Sleep(4000);

                var sub = Observable.FromEventPattern<SerialDataReceivedEventHandler, SerialDataReceivedEventArgs>(
                        x => sp.DataReceived += x,
                        x => sp.DataReceived -= x)
                    .Take(1)
                    .Timeout(TimeSpan.FromSeconds(30))
                    .Where(_ => (byte)sp.ReadByte() is 6)
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
        }

        private IObservable<Unit> ListenForButtonPresses(SerialPort comPort) => Observable.Create<Unit>(observer =>
        {
            if (!comPort.IsOpen)
            {
                comPort.Open();
            }

            var sub = Observable.FromEventPattern<SerialDataReceivedEventHandler, SerialDataReceivedEventArgs>(
                x => comPort.DataReceived += x,
                x => comPort.DataReceived -= x
            ).Where(_ => (byte)comPort.ReadByte() is 0 or 1)
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
