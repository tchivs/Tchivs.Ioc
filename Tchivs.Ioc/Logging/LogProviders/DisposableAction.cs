using System;

namespace Tchivs.Ioc.Logging.LogProviders
{
    internal class DisposableAction : IDisposable
    {
        private readonly Action _onDispose;

        public DisposableAction(Action onDispose = null)
        {
            this._onDispose = onDispose;
        }

        public void Dispose()
        {
            if (this._onDispose == null)
                return;
            this._onDispose();
        }
    }
}