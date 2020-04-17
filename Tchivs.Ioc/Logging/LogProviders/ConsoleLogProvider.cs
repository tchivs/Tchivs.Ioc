using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tchivs.Ioc.Logging.LogProviders
{
    internal sealed class ConsoleLogProvider : TcBaseLogProvider
    {
        private static readonly IDictionary<TcLogLevel, ConsoleColor> Colors = new Dictionary<TcLogLevel, ConsoleColor>
        {
            { TcLogLevel.Fatal, ConsoleColor.Red },
            { TcLogLevel.Error, ConsoleColor.Yellow },
            { TcLogLevel.Warn, ConsoleColor.Magenta },
            { TcLogLevel.Info, ConsoleColor.White },
            { TcLogLevel.Debug, ConsoleColor.Gray },
            { TcLogLevel.Trace, ConsoleColor.DarkGray }
        };

        protected override Logger GetLogger(string name) => new ColouredConsoleLogger(name).Log;

        private static string MessageFormatter(string loggerName, TcLogLevel level, object message, Exception e)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            stringBuilder.Append(" ");

            // Append a readable representation of the log level
            stringBuilder.Append(("[" + level.ToString().ToUpper() + "]").PadRight(8));
            stringBuilder.Append("(" + loggerName + ") ");

            // Append the message
            stringBuilder.Append(message);

            // Append stack trace if there is an exception
            if (e != null)
            {
                stringBuilder.Append(Environment.NewLine).Append(e.GetType());
                stringBuilder.Append(Environment.NewLine).Append(e.Message);
                stringBuilder.Append(Environment.NewLine).Append(e.StackTrace);
            }

            return stringBuilder.ToString();
        }

        public class ColouredConsoleLogger
        {
            private readonly string _name;

            public ColouredConsoleLogger(string name)
            {
                _name = name;
            }

            public bool Log(TcLogLevel logLevel, Func<string> messageFunc, Exception exception,
                params object[] formatParameters)
            {
                if (messageFunc == null) return true;

                messageFunc = LogMessageFormatter.SimulateStructuredLogging(messageFunc, formatParameters);

                Write(logLevel, messageFunc(), exception);
                return true;
            }

            protected void Write(TcLogLevel logLevel, string message, Exception e = null)
            {
                var formattedMessage = MessageFormatter(_name, logLevel, message, e);

                if (Colors.TryGetValue(logLevel, out var color))
                {
                    var originalColor = System.Console.ForegroundColor;
                    try
                    {
                        System.Console.ForegroundColor = color;
                        System.Console.WriteLine(formattedMessage);
                    }
                    finally
                    {
                        System.Console.ForegroundColor = originalColor;
                    }
                }
                else
                {
                    System.Console.WriteLine(formattedMessage);
                }
            }
        }
    }
}