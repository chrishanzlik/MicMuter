using MicMuter.WPF.Models;
using System;
using System.IO.Ports;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace MicMuter.WPF.Services
{
    public class DeviceInteractionService : IDeviceInteractionService
    {
        private readonly Subject<string> _comportChanges = new();

        private SerialPort? _currentPort;

        public IObservable<Unit> ButtonPress => this._comportChanges
            .AsObservable()
            .Where(x => x != null)
            .Select(ListenForButtonPresses)
            .Switch();

        public bool IsConnected => _currentPort != null && _currentPort.IsOpen;

        public void Connect(string comPort)
        {
            _comportChanges.OnNext(comPort);
        }

        public void SendMicrophoneState(MicState state)
        {
            if (_currentPort == null || !_currentPort.IsOpen)
            {
                throw new InvalidOperationException("SerialPort not available.");
            }

            _currentPort.Write(new byte[] { (byte)state }, 0, 1);
        }

        private IObservable<Unit> ListenForButtonPresses(string comPort) => Observable.Create<Unit>(observer =>
        {
            _currentPort = new SerialPort(comPort, 9600, Parity.None, 8, StopBits.One);

            _currentPort.Open();

            var sub = Observable.FromEventPattern<SerialDataReceivedEventHandler, SerialDataReceivedEventArgs>(
                h => _currentPort.DataReceived += h,
                h => _currentPort.DataReceived -= h
            ).Where(d => _currentPort.ReadByte() is 0 or 1)
                .Select((_) => Unit.Default)
                .Subscribe(observer);

            return () => {
                sub.Dispose();
                _currentPort.Close();
                _currentPort.Dispose();
            };
        });
    }
}
