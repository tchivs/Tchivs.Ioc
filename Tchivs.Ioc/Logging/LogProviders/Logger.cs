using System;

namespace Tchivs.Ioc.Logging.LogProviders
{
    public delegate bool Logger(
        TcLogLevel logLevel,
        Func<string> messageFunc,
        Exception exception = null,
        params object[] formatParameters);
}