using System;

namespace Tchivs.Ioc.Logging.LogProviders
{
    public abstract class TcBaseLogProvider : ITcLogProvider
    {
        private static readonly IDisposable NoopDisposableInstance = (IDisposable)new DisposableAction((Action)null);
        private readonly Lazy<TcBaseLogProvider.OpenNdc> _lazyOpenNdcMethod;
        private readonly Lazy<TcBaseLogProvider.OpenMdc> _lazyOpenMdcMethod;

        protected TcBaseLogProvider()
        {
            this._lazyOpenNdcMethod = new Lazy<TcBaseLogProvider.OpenNdc>(new Func<TcBaseLogProvider.OpenNdc>(this.GetOpenNdcMethod));
            this._lazyOpenMdcMethod = new Lazy<TcBaseLogProvider.OpenMdc>(new Func<TcBaseLogProvider.OpenMdc>(this.GetOpenMdcMethod));
        }

        public ITcLog GetLogFor(Type type)
        {
            return this.GetLogFor(type.FullName);
        }

        public ITcLog GetLogFor<T>()
        {
            return this.GetLogFor(typeof(T));
        }

        public ITcLog GetLogFor(string name)
        {
            return (ITcLog)new TcLog(this.GetLogger(name));
        }

        protected abstract Logger GetLogger(string name);

        public IDisposable OpenNestedContext(string message)
        {
            return this._lazyOpenNdcMethod.Value(message);
        }

        public IDisposable OpenMappedContext(string key, string value)
        {
            return this._lazyOpenMdcMethod.Value(key, value);
        }

        protected virtual TcBaseLogProvider.OpenNdc GetOpenNdcMethod()
        {
            return (TcBaseLogProvider.OpenNdc)(_ => TcBaseLogProvider.NoopDisposableInstance);
        }

        protected virtual TcBaseLogProvider.OpenMdc GetOpenMdcMethod()
        {
            return (TcBaseLogProvider.OpenMdc)((_, __) => TcBaseLogProvider.NoopDisposableInstance);
        }

        protected delegate IDisposable OpenNdc(string message);

        protected delegate IDisposable OpenMdc(string key, string value);
    }
}