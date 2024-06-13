using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using Akavache;
using Akavache.Core;

namespace MicMuter.WPF.Helpers
{
    public class AkavacheSuspensionDriver<TAppState> : ISuspensionDriver where TAppState : class
    {
        private const string AppStateKey = "appState";

        public AkavacheSuspensionDriver() => BlobCache.ApplicationName = "MicMuter";

        public IObservable<Unit> InvalidateState() => BlobCache.UserAccount.InvalidateObject<TAppState>(AppStateKey);

        public IObservable<object> LoadState() => BlobCache.UserAccount.GetObject<TAppState>(AppStateKey)!;

        public IObservable<Unit> SaveState(object state) => BlobCache.UserAccount.InsertObject(AppStateKey, (TAppState)state);
    }
}
