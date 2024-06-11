using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace MicMuter.WPF.Helpers
{
    public sealed class SerialPortWatcher : ISerialPortWatcher, IDisposable
    {
        private ManagementEventWatcher? _connectWatcher;
        private ManagementEventWatcher? _disconnectWatcher;
        private TaskScheduler? _taskScheduler;
        private readonly BehaviorSubject<IEnumerable<string>> _portChanges = new(SerialPort.GetPortNames().OrderBy(x => x));
        private readonly Subject<Unit> _deviceConnected = new();
        private readonly Subject<Unit> _deviceDisconnected = new ();

        public IObservable<IEnumerable<string>> PortChanges => _portChanges.AsObservable();
        public IObservable<Unit> DeviceConnected => _deviceConnected.AsObservable();
        public IObservable<Unit> DeviceDisconnected => _deviceDisconnected.AsObservable();

        public SerialPortWatcher()
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                _taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

                WqlEventQuery query = new("SELECT * FROM Win32_DeviceChangeEvent");

                _connectWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2"));
                _disconnectWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 3"));

                _connectWatcher.EventArrived += (sender, eventArgs) => {
                    CheckForNewPorts(eventArgs);
                    _deviceConnected.OnNext(Unit.Default);
                };
                _disconnectWatcher.EventArrived += (sender, eventArgs) => {
                    CheckForNewPorts(eventArgs);
                    _deviceDisconnected.OnNext(Unit.Default);
                };

                _connectWatcher.Start();
                _disconnectWatcher.Start();
            });
        }

        private void CheckForNewPorts(EventArrivedEventArgs args)
        {
            Task.Factory.StartNew(CheckForNewPortsAsync, CancellationToken.None, TaskCreationOptions.None, _taskScheduler!);
        }

        private void CheckForNewPortsAsync()
        {
            var ports = SerialPort.GetPortNames().OrderBy(x => x).Distinct().ToList();

            if (!_portChanges.Value.SequenceEqual(ports))
            {
                _portChanges.OnNext(ports);
            }
        }


        public void Dispose()
        {
            _connectWatcher?.Stop();
            _disconnectWatcher?.Stop();
        }

    }
}
