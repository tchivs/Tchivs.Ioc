using System;

namespace Tchivs.Ioc.Logging
{
    public static class LogExtensions
    {
        public static bool IsDebugEnabled(this ITcLog logger)
        {
            GuardAgainstNullLogger(logger);
            return logger.IsLogLevelEnabled(TcLogLevel.Debug);
        }

        public static bool IsErrorEnabled(this ITcLog logger)
        {
            GuardAgainstNullLogger(logger);
            return logger.IsLogLevelEnabled(TcLogLevel.Error);
        }

        public static bool IsFatalEnabled(this ITcLog logger)
        {
            GuardAgainstNullLogger(logger);
            return logger.IsLogLevelEnabled(TcLogLevel.Fatal);
        }

        public static bool IsInfoEnabled(this ITcLog logger)
        {
            GuardAgainstNullLogger(logger);
            return logger.IsLogLevelEnabled(TcLogLevel.Info);
        }

        public static bool IsTraceEnabled(this ITcLog logger)
        {
            GuardAgainstNullLogger(logger);
            return logger.IsLogLevelEnabled(TcLogLevel.Trace);
        }

        public static bool IsWarnEnabled(this ITcLog logger)
        {
            GuardAgainstNullLogger(logger);
            return logger.IsLogLevelEnabled(TcLogLevel.Warn);
        }

        public static void Debug(this ITcLog logger, Func<string> messageFunc)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(TcLogLevel.Debug, messageFunc);
        }

        public static void Debug(this ITcLog logger, string message)
        {
            if (logger.IsDebugEnabled())
            {
                logger.Log(TcLogLevel.Debug, message.AsFunc());
            }
        }

        public static void Debug(this ITcLog logger, string message, params object[] args)
        {
            logger.DebugFormat(message, args);
        }

        public static void Debug(this ITcLog logger, Exception exception, string message, params object[] args)
        {
            logger.DebugException(message, exception, args);
        }

        public static void DebugFormat(this ITcLog logger, string message, params object[] args)
        {
            if (logger.IsDebugEnabled())
            {
                logger.LogFormat(TcLogLevel.Debug, message, args);
            }
        }

        public static void DebugException(this ITcLog logger, string message, Exception exception)
        {
            if (logger.IsDebugEnabled())
            {
                logger.Log(TcLogLevel.Debug, message.AsFunc(), exception);
            }
        }

        public static void DebugException(this ITcLog logger, string message, Exception exception, params object[] formatParams)
        {
            if (logger.IsDebugEnabled())
            {
                logger.Log(TcLogLevel.Debug, message.AsFunc(), exception, formatParams);
            }
        }

        public static void Error(this ITcLog logger, Func<string> messageFunc)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(TcLogLevel.Error, messageFunc);
        }

        public static void Error(this ITcLog logger, string message)
        {
            if (logger.IsErrorEnabled())
            {
                logger.Log(TcLogLevel.Error, message.AsFunc());
            }
        }

        public static void Error(this ITcLog logger, string message, params object[] args)
        {
            logger.ErrorFormat(message, args);
        }

        public static void Error(this ITcLog logger, Exception exception, string message, params object[] args)
        {
            logger.ErrorException(message, exception, args);
        }

        public static void ErrorFormat(this ITcLog logger, string message, params object[] args)
        {
            if (logger.IsErrorEnabled())
            {
                logger.LogFormat(TcLogLevel.Error, message, args);
            }
        }

        public static void ErrorException(this ITcLog logger, string message, Exception exception, params object[] formatParams)
        {
            if (logger.IsErrorEnabled())
            {
                logger.Log(TcLogLevel.Error, message.AsFunc(), exception, formatParams);
            }
        }

        public static void Fatal(this ITcLog logger, Func<string> messageFunc)
        {
            logger.Log(TcLogLevel.Fatal, messageFunc);
        }

        public static void Fatal(this ITcLog logger, string message)
        {
            if (logger.IsFatalEnabled())
            {
                logger.Log(TcLogLevel.Fatal, message.AsFunc());
            }
        }

        public static void Fatal(this ITcLog logger, string message, params object[] args)
        {
            logger.FatalFormat(message, args);
        }

        public static void Fatal(this ITcLog logger, Exception exception, string message, params object[] args)
        {
            logger.FatalException(message, exception, args);
        }

        public static void FatalFormat(this ITcLog logger, string message, params object[] args)
        {
            if (logger.IsFatalEnabled())
            {
                logger.LogFormat(TcLogLevel.Fatal, message, args);
            }
        }

        public static void FatalException(this ITcLog logger, string message, Exception exception, params object[] formatParams)
        {
            if (logger.IsFatalEnabled())
            {
                logger.Log(TcLogLevel.Fatal, message.AsFunc(), exception, formatParams);
            }
        }

        public static void Info(this ITcLog logger, Func<string> messageFunc)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(TcLogLevel.Info, messageFunc);
        }

        public static void Info(this ITcLog logger, string message)
        {
            if (logger.IsInfoEnabled())
            {
                logger.Log(TcLogLevel.Info, message.AsFunc());
            }
        }

        public static void Info(this ITcLog logger, string message, params object[] args)
        {
            logger.InfoFormat(message, args);
        }

        public static void Info(this ITcLog logger, Exception exception, string message, params object[] args)
        {
            logger.InfoException(message, exception, args);
        }

        public static void InfoFormat(this ITcLog logger, string message, params object[] args)
        {
            if (logger.IsInfoEnabled())
            {
                logger.LogFormat(TcLogLevel.Info, message, args);
            }
        }

        public static void InfoException(this ITcLog logger, string message, Exception exception, params object[] formatParams)
        {
            if (logger.IsInfoEnabled())
            {
                logger.Log(TcLogLevel.Info, message.AsFunc(), exception, formatParams);
            }
        }

        public static void Trace(this ITcLog logger, Func<string> messageFunc)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(TcLogLevel.Trace, messageFunc);
        }

        public static void Trace(this ITcLog logger, string message)
        {
            if (logger.IsTraceEnabled())
            {
                logger.Log(TcLogLevel.Trace, message.AsFunc());
            }
        }

        public static void Trace(this ITcLog logger, string message, params object[] args)
        {
            logger.TraceFormat(message, args);
        }

        public static void Trace(this ITcLog logger, Exception exception, string message, params object[] args)
        {
            logger.TraceException(message, exception, args);
        }

        public static void TraceFormat(this ITcLog logger, string message, params object[] args)
        {
            if (logger.IsTraceEnabled())
            {
                logger.LogFormat(TcLogLevel.Trace, message, args);
            }
        }

        public static void TraceException(this ITcLog logger, string message, Exception exception, params object[] formatParams)
        {
            if (logger.IsTraceEnabled())
            {
                logger.Log(TcLogLevel.Trace, message.AsFunc(), exception, formatParams);
            }
        }

        public static void Warn(this ITcLog logger, Func<string> messageFunc)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(TcLogLevel.Warn, messageFunc);
        }

        public static void Warn(this ITcLog logger, string message)
        {
            if (logger.IsWarnEnabled())
            {
                logger.Log(TcLogLevel.Warn, message.AsFunc());
            }
        }

        public static void Warn(this ITcLog logger, string message, params object[] args)
        {
            logger.WarnFormat(message, args);
        }

        public static void Warn(this ITcLog logger, Exception exception, string message, params object[] args)
        {
            logger.WarnException(message, exception, args);
        }

        public static void WarnFormat(this ITcLog logger, string message, params object[] args)
        {
            if (logger.IsWarnEnabled())
            {
                logger.LogFormat(TcLogLevel.Warn, message, args);
            }
        }

        public static void WarnException(this ITcLog logger, string message, Exception exception, params object[] formatParams)
        {
            if (logger.IsWarnEnabled())
            {
                logger.Log(TcLogLevel.Warn, message.AsFunc(), exception, formatParams);
            }
        }

        // ReSharper disable once UnusedParameter.Local
        private static void GuardAgainstNullLogger(ITcLog logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
        }

        private static void LogFormat(this ITcLog logger, TcLogLevel logLevel, string message, params object[] args)
        {
            logger.Log(logLevel, message.AsFunc(), null, args);
        }

        // Avoid the closure allocation, see https://gist.github.com/AArnott/d285feef75c18f6ecd2b
        private static Func<T> AsFunc<T>(this T value) where T : class
        {
            return value.Return;
        }

        private static T Return<T>(this T value)
        {
            return value;
        }
    }
}