using MicMuter.WPF.Models;
using ReactiveUI;
using System;

namespace MicMuter.WPF.Helpers
{
    public static class Interactions
    {
        public static readonly Interaction<Exception, ErrorRecoveryOption> ConnectionErrorRetryInteraction = new();
    }
}
