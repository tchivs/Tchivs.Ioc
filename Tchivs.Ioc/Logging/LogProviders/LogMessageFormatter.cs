using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Tchivs.Ioc.Logging.LogProviders
{
    internal static class LogMessageFormatter
    {
        private static readonly Regex Pattern = new Regex("(?<!{){@?(?<arg>[^\\d{][^ }]*)}");

        /// <summary>
        /// Some logging frameworks support structured logging, such as serilog. This will allow you to add names to structured data in a format string:
        /// For example: Log("Log message to {user}", user). This only works with serilog, but as the user of LibLog, you don't know if serilog is actually
        /// used. So, this class simulates that. it will replace any text in {curly braces} with an index number.
        ///
        /// "Log {message} to {user}" would turn into =&gt; "Log {0} to {1}". Then the format parameters are handled using regular .net string.Format.
        /// </summary>
        /// <param name="messageBuilder">The message builder.</param>
        /// <param name="formatParameters">The format parameters.</param>
        /// <returns></returns>
        public static Func<string> SimulateStructuredLogging(
            Func<string> messageBuilder,
            object[] formatParameters)
        {
            return formatParameters == null || formatParameters.Length == 0 ? messageBuilder : (Func<string>)(() => LogMessageFormatter.FormatStructuredMessage(messageBuilder(), formatParameters, out IEnumerable<string> _));
        }

        private static string ReplaceFirst(string text, string search, string replace)
        {
            int length = text.IndexOf(search, StringComparison.Ordinal);
            return length < 0 ? text : text.Substring(0, length) + replace + text.Substring(length + search.Length);
        }

        public static string FormatStructuredMessage(
            string targetMessage,
            object[] formatParameters,
            out IEnumerable<string> patternMatches)
        {
            if (formatParameters == null || formatParameters.Length == 0)
            {
                patternMatches = Enumerable.Empty<string>();
                return targetMessage;
            }
            List<string> stringList = (List<string>)null;
            foreach (Match match in LogMessageFormatter.Pattern.Matches(targetMessage))
            {
                string s = match.Groups["arg"].Value;
                if (!int.TryParse(s, out int _))
                {
                    stringList = stringList ?? new List<string>(formatParameters.Length);
                    int num = stringList.IndexOf(s);
                    if (num == -1)
                    {
                        num = stringList.Count;
                        stringList.Add(s);
                    }
                    targetMessage = LogMessageFormatter.ReplaceFirst(targetMessage, match.Value, "{" + num.ToString() + match.Groups["format"].Value + "}");
                }
            }
            patternMatches = (IEnumerable<string>)stringList ?? Enumerable.Empty<string>();
            try
            {
                return string.Format((IFormatProvider)CultureInfo.InvariantCulture, targetMessage, formatParameters);
            }
            catch (FormatException ex)
            {
                throw new FormatException("The input string '" + targetMessage + "' could not be formatted using string.Format", (Exception)ex);
            }
        }
    }
}