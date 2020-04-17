using System;
using Tchivs.Ioc.Logging.LogProviders;

namespace Tchivs.Ioc.Logging
{
    public interface ITcLog
    {
        bool Log(TcLogLevel logLevel, Func<string> messageFunc, Exception exception = null, params object[] formatParameters);

        bool IsLogLevelEnabled(TcLogLevel logLevel);
    }

    public enum MvxLogProviderType
    {
        Console,
        EntLib,
        Log4Net,
        Loupe,
        NLog,
        Serilog
    }

    public interface ITcLogProvider
    {
        ITcLog GetLogFor(Type type);

        ITcLog GetLogFor<T>();

        ITcLog GetLogFor(string name);

        IDisposable OpenNestedContext(string message);

        IDisposable OpenMappedContext(string key, string value);
    }

    internal class TcLog : ITcLog
    {
        internal static ITcLog Instance { get; set; }

        internal const string FailedToGenerateLogMessage = "Failed to generate log message";

        private readonly Logger _logger;

        internal TcLog(Logger logger)
        {
            _logger = logger;
        }

        public bool IsLogLevelEnabled(TcLogLevel logLevel) => _logger(logLevel, null);

        public bool Log(TcLogLevel logLevel, Func<string> messageFunc, Exception exception = null, params object[] formatParameters)
        {
            if (messageFunc == null)
                return _logger(logLevel, null);

            Func<string> wrappedMessageFunc = () =>
            {
                try
                {
                    return messageFunc();
                }
                catch (Exception ex)
                {
                    Log(TcLogLevel.Error, () => FailedToGenerateLogMessage, ex);
                }

                return null;
            };

            return _logger(logLevel, wrappedMessageFunc, exception, formatParameters);
        }
    }
}