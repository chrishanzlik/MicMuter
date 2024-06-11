using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;

namespace MicMuter.WPF.Helpers
{
    public interface ISerialPortWatcher
    {
        IObservable<IEnumerable<string>> PortChanges { get; }
        IObservable<Unit> DeviceConnected { get; }
        IObservable<Unit> DeviceDisconnected { get; }
    }
}
