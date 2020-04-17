using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Tchivs.Ioc.Logging.LogProviders
{
    internal class NLogLogProvider : TcBaseLogProvider
    {
        private readonly Func<string, object> _getLoggerByNameDelegate;

        public NLogLogProvider()
        {
            if (!IsLoggerAvailable())
            {
                throw new InvalidOperationException("NLog.LogManager not found");
            }
            _getLoggerByNameDelegate = GetGetLoggerMethodCall();
        }

        protected override Logger GetLogger(string name)
            => new NLogLogger(_getLoggerByNameDelegate(name)).Log;

        public static bool IsLoggerAvailable()
            => GetLogManagerType() != null;

        protected override OpenNdc GetOpenNdcMethod()
        {
            ParameterExpression messageParam = Expression.Parameter(typeof(string), "message");

            Type ndlcContextType = Type.GetType("NLog.NestedDiagnosticsLogicalContext, NLog");
            if (ndlcContextType != null)
            {
                MethodInfo pushObjectMethod = ndlcContextType.GetMethod("PushObject", new[] { typeof(object) });
                if (pushObjectMethod != null)
                {
                    // NLog 4.6 introduces PushObject with correct handling of logical callcontext (NDLC)
                    MethodCallExpression pushObjectMethodCall = Expression.Call(null, pushObjectMethod, messageParam);
                    return Expression.Lambda<OpenNdc>(pushObjectMethodCall, messageParam).Compile();
                }
            }

            Type ndcContextType = Type.GetType("NLog.NestedDiagnosticsContext, NLog");
            MethodInfo pushMethod = ndcContextType.GetMethod("Push", new[] { typeof(string) });
            MethodCallExpression pushMethodCall = Expression.Call(null, pushMethod, messageParam);
            return Expression.Lambda<OpenNdc>(pushMethodCall, messageParam).Compile();
        }

        protected override OpenMdc GetOpenMdcMethod()
        {
            ParameterExpression keyParam = Expression.Parameter(typeof(string), "key");

            Type ndlcContextType = Type.GetType("NLog.NestedDiagnosticsLogicalContext, NLog");
            if (ndlcContextType != null)
            {
                MethodInfo pushObjectMethod = ndlcContextType.GetMethod("PushObject", new[] { typeof(object) });
                if (pushObjectMethod != null)
                {
                    // NLog 4.6 introduces SetScoped with correct handling of logical callcontext (MDLC)
                    Type mdlcContextType = Type.GetType("NLog.MappedDiagnosticsLogicalContext, NLog");
                    if (mdlcContextType != null)
                    {
                        MethodInfo setScopedMethod = mdlcContextType.GetMethod("SetScoped", new[] { typeof(string), typeof(object) });
                        if (setScopedMethod != null)
                        {
                            var valueObjParam = Expression.Parameter(typeof(object), "value");
                            var setScopedMethodCall = Expression.Call(null, setScopedMethod, keyParam, valueObjParam);
                            var setMethodLambda = Expression.Lambda<Func<string, object, IDisposable>>(setScopedMethodCall, keyParam, valueObjParam).Compile();
                            return (key, value) => setMethodLambda(key, value);
                        }
                    }
                }
            }

            Type mdcContextType = Type.GetType("NLog.MappedDiagnosticsContext, NLog");
            MethodInfo setMethod = mdcContextType.GetMethod("Set", new[] { typeof(string), typeof(string) });
            MethodInfo removeMethod = mdcContextType.GetMethod("Remove", new[] { typeof(string) });
            ParameterExpression valueParam = Expression.Parameter(typeof(string), "value");
            MethodCallExpression setMethodCall = Expression.Call(null, setMethod, keyParam, valueParam);
            MethodCallExpression removeMethodCall = Expression.Call(null, removeMethod, keyParam);

            Action<string, string> set = Expression
                .Lambda<Action<string, string>>(setMethodCall, keyParam, valueParam)
                .Compile();
            Action<string> remove = Expression
                .Lambda<Action<string>>(removeMethodCall, keyParam)
                .Compile();

            return (key, value) =>
            {
                set(key, value);
                return new DisposableAction(() => remove(key));
            };
        }

        private static Type GetLogManagerType()
            => Type.GetType("NLog.LogManager, NLog");

        private static Func<string, object> GetGetLoggerMethodCall()
        {
            Type logManagerType = GetLogManagerType();
            MethodInfo method = logManagerType.GetMethod("GetLogger", new[] { typeof(string) });
            ParameterExpression nameParam = Expression.Parameter(typeof(string), "name");
            MethodCallExpression methodCall = Expression.Call(null, method, nameParam);
            return Expression.Lambda<Func<string, object>>(methodCall, nameParam).Compile();
        }

        internal class NLogLogger
        {
            private readonly object _logger;

            private static readonly Func<string, object, string, object[], Exception, object> _logEventInfoFact;

            private static readonly object _levelTrace;
            private static readonly object _levelDebug;
            private static readonly object _levelInfo;
            private static readonly object _levelWarn;
            private static readonly object _levelError;
            private static readonly object _levelFatal;

            private static bool _structuredLoggingEnabled;

            private delegate string LoggerNameDelegate(object logger);

            private delegate void LogEventDelegate(object logger, Type wrapperType, object logEvent);

            private delegate bool IsEnabledDelegate(object logger);

            private delegate void LogDelegate(object logger, string message);

            private delegate void LogExceptionDelegate(object logger, string message, Exception exception);

            private static readonly LoggerNameDelegate _loggerNameDelegate;
            private static readonly LogEventDelegate _logEventDelegate;

            private static readonly IsEnabledDelegate _isTraceEnabledDelegate;
            private static readonly IsEnabledDelegate _isDebugEnabledDelegate;
            private static readonly IsEnabledDelegate _isInfoEnabledDelegate;
            private static readonly IsEnabledDelegate _isWarnEnabledDelegate;
            private static readonly IsEnabledDelegate _isErrorEnabledDelegate;
            private static readonly IsEnabledDelegate _isFatalEnabledDelegate;

            private static readonly LogDelegate _traceDelegate;
            private static readonly LogDelegate _debugDelegate;
            private static readonly LogDelegate _infoDelegate;
            private static readonly LogDelegate _warnDelegate;
            private static readonly LogDelegate _errorDelegate;
            private static readonly LogDelegate _fatalDelegate;

            private static readonly LogExceptionDelegate _traceExceptionDelegate;
            private static readonly LogExceptionDelegate _debugExceptionDelegate;
            private static readonly LogExceptionDelegate _infoExceptionDelegate;
            private static readonly LogExceptionDelegate _warnExceptionDelegate;
            private static readonly LogExceptionDelegate _errorExceptionDelegate;
            private static readonly LogExceptionDelegate _fatalExceptionDelegate;

            static NLogLogger()
            {
                try
                {
                    var logEventLevelType = Type.GetType("NLog.LogLevel, NLog");
                    if (logEventLevelType == null)
                    {
                        throw new InvalidOperationException("Type NLog.LogLevel was not found.");
                    }

                    var levelFields = logEventLevelType.GetFields().ToList();
                    _levelTrace = levelFields.First(x => x.Name == "Trace").GetValue(null);
                    _levelDebug = levelFields.First(x => x.Name == "Debug").GetValue(null);
                    _levelInfo = levelFields.First(x => x.Name == "Info").GetValue(null);
                    _levelWarn = levelFields.First(x => x.Name == "Warn").GetValue(null);
                    _levelError = levelFields.First(x => x.Name == "Error").GetValue(null);
                    _levelFatal = levelFields.First(x => x.Name == "Fatal").GetValue(null);

                    var logEventInfoType = Type.GetType("NLog.LogEventInfo, NLog");
                    if (logEventInfoType == null)
                    {
                        throw new InvalidOperationException("Type NLog.LogEventInfo was not found.");
                    }

                    var loggingEventConstructor = logEventInfoType.GetConstructor(new[]
                    {
                        logEventLevelType,
                        typeof(string),
                        typeof(IFormatProvider),
                        typeof(string),
                        typeof(object[]),
                        typeof(Exception),
                    });
                    ParameterExpression loggerNameParam = Expression.Parameter(typeof(string));
                    ParameterExpression levelParam = Expression.Parameter(typeof(object));
                    ParameterExpression messageParam = Expression.Parameter(typeof(string));
                    ParameterExpression messageArgsParam = Expression.Parameter(typeof(object[]));
                    ParameterExpression exceptionParam = Expression.Parameter(typeof(Exception));
                    UnaryExpression levelCast = Expression.Convert(levelParam, logEventLevelType);

                    NewExpression newLoggingEventExpression =
                        Expression.New(loggingEventConstructor,
                            levelCast,
                            loggerNameParam,
                            Expression.Constant(null, typeof(IFormatProvider)),
                            messageParam,
                            messageArgsParam,
                            exceptionParam
                        );

                    _logEventInfoFact = Expression.Lambda<Func<string, object, string, object[], Exception, object>>(
                        newLoggingEventExpression,
                        loggerNameParam, levelParam, messageParam, messageArgsParam, exceptionParam).Compile();

                    Type loggerType = Type.GetType("NLog.Logger, NLog");

                    _loggerNameDelegate = GetLoggerNameDelegate(loggerType);

                    _logEventDelegate = GetLogEventDelegate(loggerType, logEventInfoType);

                    _isTraceEnabledDelegate = GetIsEnabledDelegate(loggerType, "IsTraceEnabled");
                    _isDebugEnabledDelegate = GetIsEnabledDelegate(loggerType, "IsDebugEnabled");
                    _isInfoEnabledDelegate = GetIsEnabledDelegate(loggerType, "IsInfoEnabled");
                    _isWarnEnabledDelegate = GetIsEnabledDelegate(loggerType, "IsWarnEnabled");
                    _isErrorEnabledDelegate = GetIsEnabledDelegate(loggerType, "IsErrorEnabled");
                    _isFatalEnabledDelegate = GetIsEnabledDelegate(loggerType, "IsFatalEnabled");

                    _traceDelegate = GetLogDelegate(loggerType, "Trace");
                    _debugDelegate = GetLogDelegate(loggerType, "Debug");
                    _infoDelegate = GetLogDelegate(loggerType, "Info");
                    _warnDelegate = GetLogDelegate(loggerType, "Warn");
                    _errorDelegate = GetLogDelegate(loggerType, "Error");
                    _fatalDelegate = GetLogDelegate(loggerType, "Fatal");

                    _traceExceptionDelegate = GetLogExceptionDelegate(loggerType, "TraceException");
                    _debugExceptionDelegate = GetLogExceptionDelegate(loggerType, "DebugException");
                    _infoExceptionDelegate = GetLogExceptionDelegate(loggerType, "InfoException");
                    _warnExceptionDelegate = GetLogExceptionDelegate(loggerType, "WarnException");
                    _errorExceptionDelegate = GetLogExceptionDelegate(loggerType, "ErrorException");
                    _fatalExceptionDelegate = GetLogExceptionDelegate(loggerType, "FatalException");

                    _structuredLoggingEnabled = IsStructuredLoggingEnabled();
                }
                catch { }
            }

            private static IsEnabledDelegate GetIsEnabledDelegate(Type loggerType, string propertyName)
            {
                var isEnabledPropertyInfo = loggerType.GetProperty(propertyName);
                var instanceParam = Expression.Parameter(typeof(object));
                var instanceCast = Expression.Convert(instanceParam, loggerType);
                var propertyCall = Expression.Property(instanceCast, isEnabledPropertyInfo);
                return Expression.Lambda<IsEnabledDelegate>(propertyCall, instanceParam).Compile();
            }

            private static LoggerNameDelegate GetLoggerNameDelegate(Type loggerType)
            {
                var isEnabledPropertyInfo = loggerType.GetProperty("Name");
                var instanceParam = Expression.Parameter(typeof(object));
                var instanceCast = Expression.Convert(instanceParam, loggerType);
                var propertyCall = Expression.Property(instanceCast, isEnabledPropertyInfo);
                return Expression.Lambda<LoggerNameDelegate>(propertyCall, instanceParam).Compile();
            }

            private static LogDelegate GetLogDelegate(Type loggerType, string name)
            {
                var logMethodInfo = loggerType.GetMethod(name, new Type[] { typeof(string) });
                var instanceParam = Expression.Parameter(typeof(object));
                var instanceCast = Expression.Convert(instanceParam, loggerType);
                var messageParam = Expression.Parameter(typeof(string));
                var logCall = Expression.Call(instanceCast, logMethodInfo, messageParam);
                return Expression.Lambda<LogDelegate>(logCall, instanceParam, messageParam).Compile();
            }

            private static LogEventDelegate GetLogEventDelegate(Type loggerType, Type logEventType)
            {
                var logMethodInfo = loggerType.GetMethod("Log", new Type[] { typeof(Type), logEventType });
                var instanceParam = Expression.Parameter(typeof(object));
                var instanceCast = Expression.Convert(instanceParam, loggerType);
                var loggerTypeParam = Expression.Parameter(typeof(Type));
                var logEventParam = Expression.Parameter(typeof(object));
                var logEventCast = Expression.Convert(logEventParam, logEventType);
                var logCall = Expression.Call(instanceCast, logMethodInfo, loggerTypeParam, logEventCast);
                return Expression.Lambda<LogEventDelegate>(logCall, instanceParam, loggerTypeParam, logEventParam).Compile();
            }

            private static LogExceptionDelegate GetLogExceptionDelegate(Type loggerType, string name)
            {
                var logMethodInfo = loggerType.GetMethod(name, new Type[] { typeof(string), typeof(Exception) });
                var instanceParam = Expression.Parameter(typeof(object));
                var instanceCast = Expression.Convert(instanceParam, loggerType);
                var messageParam = Expression.Parameter(typeof(string));
                var exceptionParam = Expression.Parameter(typeof(Exception));
                var logCall = Expression.Call(instanceCast, logMethodInfo, messageParam, exceptionParam);
                return Expression.Lambda<LogExceptionDelegate>(logCall, instanceParam, messageParam, exceptionParam).Compile();
            }

            internal NLogLogger(object logger)
            {
                _logger = logger;
            }

            [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
            public bool Log(TcLogLevel logLevel, Func<string> messageFunc, Exception exception, params object[] formatParameters)
            {
                if (messageFunc == null)
                {
                    return IsLogLevelEnable(logLevel);
                }

                if (_logEventInfoFact != null)
                {
                    if (IsLogLevelEnable(logLevel))
                    {
                        var formatMessage = messageFunc();
                        if (!_structuredLoggingEnabled)
                        {
                            IEnumerable<string> _;
                            formatMessage =
                                LogMessageFormatter.FormatStructuredMessage(formatMessage,
                                    formatParameters,
                                    out _);
                            formatParameters = null; // Has been formatted, no need for parameters
                        }

                        var nlogLevel = TranslateLevel(logLevel);
                        var nlogEvent = _logEventInfoFact(_loggerNameDelegate(_logger), nlogLevel, formatMessage, formatParameters, exception);
                        _logEventDelegate(_logger, typeof(ITcLog), nlogEvent);
                        return true;
                    }

                    return false;
                }

                messageFunc = LogMessageFormatter.SimulateStructuredLogging(messageFunc, formatParameters);
                if (exception != null)
                {
                    return LogException(logLevel, messageFunc, exception);
                }
                switch (logLevel)
                {
                    case TcLogLevel.Debug:
                        if (_isDebugEnabledDelegate(_logger))
                        {
                            _debugDelegate(_logger, messageFunc());
                            return true;
                        }

                        break;

                    case TcLogLevel.Info:
                        if (_isInfoEnabledDelegate(_logger))
                        {
                            _infoDelegate(_logger, messageFunc());
                            return true;
                        }

                        break;

                    case TcLogLevel.Warn:
                        if (_isWarnEnabledDelegate(_logger))
                        {
                            _warnDelegate(_logger, messageFunc());
                            return true;
                        }

                        break;

                    case TcLogLevel.Error:
                        if (_isErrorEnabledDelegate(_logger))
                        {
                            _errorDelegate(_logger, messageFunc());
                            return true;
                        }

                        break;

                    case TcLogLevel.Fatal:
                        if (_isFatalEnabledDelegate(_logger))
                        {
                            _fatalDelegate(_logger, messageFunc());
                            return true;
                        }

                        break;

                    default:
                        if (_isTraceEnabledDelegate(_logger))
                        {
                            _traceDelegate(_logger, messageFunc());
                            return true;
                        }

                        break;
                }

                return false;
            }

            [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
            private bool LogException(TcLogLevel logLevel, Func<string> messageFunc, Exception exception)
            {
                switch (logLevel)
                {
                    case TcLogLevel.Debug:
                        if (_isDebugEnabledDelegate(_logger))
                        {
                            _debugExceptionDelegate(_logger, messageFunc(), exception);
                            return true;
                        }

                        break;

                    case TcLogLevel.Info:
                        if (_isInfoEnabledDelegate(_logger))
                        {
                            _infoExceptionDelegate(_logger, messageFunc(), exception);
                            return true;
                        }

                        break;

                    case TcLogLevel.Warn:
                        if (_isWarnEnabledDelegate(_logger))
                        {
                            _warnExceptionDelegate(_logger, messageFunc(), exception);
                            return true;
                        }

                        break;

                    case TcLogLevel.Error:
                        if (_isErrorEnabledDelegate(_logger))
                        {
                            _errorExceptionDelegate(_logger, messageFunc(), exception);
                            return true;
                        }

                        break;

                    case TcLogLevel.Fatal:
                        if (_isFatalEnabledDelegate(_logger))
                        {
                            _fatalExceptionDelegate(_logger, messageFunc(), exception);
                            return true;
                        }

                        break;

                    default:
                        if (_isTraceEnabledDelegate(_logger))
                        {
                            _traceExceptionDelegate(_logger, messageFunc(), exception);
                            return true;
                        }

                        break;
                }

                return false;
            }

            private bool IsLogLevelEnable(TcLogLevel logLevel)
            {
                switch (logLevel)
                {
                    case TcLogLevel.Debug:
                        return _isDebugEnabledDelegate(_logger);

                    case TcLogLevel.Info:
                        return _isInfoEnabledDelegate(_logger);

                    case TcLogLevel.Warn:
                        return _isWarnEnabledDelegate(_logger);

                    case TcLogLevel.Error:
                        return _isErrorEnabledDelegate(_logger);

                    case TcLogLevel.Fatal:
                        return _isFatalEnabledDelegate(_logger);

                    default:
                        return _isTraceEnabledDelegate(_logger);
                }
            }

            private object TranslateLevel(TcLogLevel logLevel)
            {
                switch (logLevel)
                {
                    case TcLogLevel.Trace:
                        return _levelTrace;

                    case TcLogLevel.Debug:
                        return _levelDebug;

                    case TcLogLevel.Info:
                        return _levelInfo;

                    case TcLogLevel.Warn:
                        return _levelWarn;

                    case TcLogLevel.Error:
                        return _levelError;

                    case TcLogLevel.Fatal:
                        return _levelFatal;

                    default:
                        throw new ArgumentOutOfRangeException("logLevel", logLevel, null);
                }
            }

            private static bool IsStructuredLoggingEnabled()
            {
                Type configFactoryType = Type.GetType("NLog.Config.ConfigurationItemFactory, NLog");
                if (configFactoryType != null)
                {
                    PropertyInfo parseMessagesProperty = configFactoryType.GetProperty("ParseMessageTemplates");
                    if (parseMessagesProperty != null)
                    {
                        var defaultProperty = configFactoryType.GetProperty("Default");
                        if (defaultProperty != null)
                        {
                            var configFactoryDefault = defaultProperty.GetValue(null, null);
                            if (configFactoryDefault != null)
                            {
                                var parseMessageTemplates =
                                    parseMessagesProperty.GetValue(configFactoryDefault, null) as bool?;
                                if (parseMessageTemplates != false) return true;
                            }
                        }
                    }
                }

                return false;
            }
        }
    }

    internal class Log4NetLogProvider : TcBaseLogProvider
    {
        private readonly Func<Assembly, string, object> _getLoggerByNameDelegate;

        public Log4NetLogProvider()
        {
            if (!IsLoggerAvailable())
            {
                throw new InvalidOperationException("log4net.LogManager not found");
            }

            _getLoggerByNameDelegate = GetGetLoggerMethodCall();
        }

        protected override Logger GetLogger(string name)
        {
            Assembly assembly = Assembly.GetCallingAssembly();
            return new Log4NetLogger(_getLoggerByNameDelegate(assembly, name)).Log;
        }

        internal static bool IsLoggerAvailable()
            => GetLogManagerType() != null;

        protected override OpenNdc GetOpenNdcMethod()
        {
            Type logicalThreadContextType = Type.GetType("log4net.LogicalThreadContext, log4net");
            PropertyInfo stacksProperty = logicalThreadContextType.GetProperty("Stacks");
            Type logicalThreadContextStacksType = stacksProperty.PropertyType;
            PropertyInfo stacksIndexerProperty = logicalThreadContextStacksType.GetProperty("Item");
            Type stackType = stacksIndexerProperty.PropertyType;
            MethodInfo pushMethod = stackType.GetMethod("Push");

            ParameterExpression messageParameter =
                Expression.Parameter(typeof(string), "message");

            // message => LogicalThreadContext.Stacks.Item["NDC"].Push(message);
            MethodCallExpression callPushBody =
                Expression.Call(
                    Expression.Property(Expression.Property(null, stacksProperty),
                                        stacksIndexerProperty,
                                        Expression.Constant("NDC")),
                    pushMethod,
                    messageParameter);

            OpenNdc result =
                Expression.Lambda<OpenNdc>(callPushBody, messageParameter)
                          .Compile();

            return result;
        }

        protected override OpenMdc GetOpenMdcMethod()
        {
            Type logicalThreadContextType = Type.GetType("log4net.LogicalThreadContext, log4net");
            PropertyInfo propertiesProperty = logicalThreadContextType.GetProperty("Properties");
            Type logicalThreadContextPropertiesType = propertiesProperty.PropertyType;
            PropertyInfo propertiesIndexerProperty = logicalThreadContextPropertiesType.GetProperty("Item");

            MethodInfo removeMethod = logicalThreadContextPropertiesType.GetMethod("Remove");

            ParameterExpression keyParam = Expression.Parameter(typeof(string), "key");
            ParameterExpression valueParam = Expression.Parameter(typeof(string), "value");

            MemberExpression propertiesExpression = Expression.Property(null, propertiesProperty);

            // (key, value) => LogicalThreadContext.Properties.Item[key] = value;
            BinaryExpression setProperties = Expression.Assign(Expression.Property(propertiesExpression, propertiesIndexerProperty, keyParam), valueParam);

            // key => LogicalThreadContext.Properties.Remove(key);
            MethodCallExpression removeMethodCall = Expression.Call(propertiesExpression, removeMethod, keyParam);

            Action<string, string> set = Expression
                .Lambda<Action<string, string>>(setProperties, keyParam, valueParam)
                .Compile();

            Action<string> remove = Expression
                .Lambda<Action<string>>(removeMethodCall, keyParam)
                .Compile();

            return (key, value) =>
            {
                set(key, value);
                return new DisposableAction(() => remove(key));
            };
        }

        private static Type GetLogManagerType()
            => Type.GetType("log4net.LogManager, log4net");

        private static Func<Assembly, string, object> GetGetLoggerMethodCall()
        {
            Type logManagerType = GetLogManagerType();
            MethodInfo method = logManagerType.GetMethod("GetLogger", new[] { typeof(Assembly), typeof(string) });
            ParameterExpression repositoryAssemblyParam = Expression.Parameter(typeof(Assembly), "repositoryAssembly");
            ParameterExpression nameParam = Expression.Parameter(typeof(string), "name");
            MethodCallExpression methodCall = Expression.Call(null, method, repositoryAssemblyParam, nameParam);
            return Expression.Lambda<Func<Assembly, string, object>>(methodCall, repositoryAssemblyParam, nameParam).Compile();
        }

        internal class Log4NetLogger
        {
            private readonly object _logger;
            private static Type s_callerStackBoundaryType;
            private static readonly object CallerStackBoundaryTypeSync = new object();

            private static readonly object _levelDebug;
            private static readonly object _levelInfo;
            private static readonly object _levelWarn;
            private static readonly object _levelError;
            private static readonly object _levelFatal;
            private static readonly Func<object, object, bool> _isEnabledForDelegate;
            private static readonly Action<object, object> _logDelegate;
            private static readonly Func<object, Type, object, string, Exception, object> _createLoggingEvent;
            private static readonly Action<object, string, object> _loggingEventPropertySetter;

            static Log4NetLogger()
            {
                var logEventLevelType = Type.GetType("log4net.Core.Level, log4net");
                if (logEventLevelType == null)
                {
                    throw new InvalidOperationException("Type log4net.Core.Level was not found.");
                }

                var levelFields = logEventLevelType.GetFields().ToList();
                _levelDebug = levelFields.First(x => x.Name == "Debug").GetValue(null);
                _levelInfo = levelFields.First(x => x.Name == "Info").GetValue(null);
                _levelWarn = levelFields.First(x => x.Name == "Warn").GetValue(null);
                _levelError = levelFields.First(x => x.Name == "Error").GetValue(null);
                _levelFatal = levelFields.First(x => x.Name == "Fatal").GetValue(null);

                // Func<object, object, bool> isEnabledFor = (logger, level) => { return ((log4net.Core.ILogger)logger).IsEnabled(level); }
                var loggerType = Type.GetType("log4net.Core.ILogger, log4net");
                if (loggerType == null)
                {
                    throw new InvalidOperationException("Type log4net.Core.ILogger, was not found.");
                }
                ParameterExpression instanceParam = Expression.Parameter(typeof(object));
                UnaryExpression instanceCast = Expression.Convert(instanceParam, loggerType);
                ParameterExpression levelParam = Expression.Parameter(typeof(object));
                UnaryExpression levelCast = Expression.Convert(levelParam, logEventLevelType);
                _isEnabledForDelegate = GetIsEnabledFor(loggerType, logEventLevelType, instanceCast, levelCast, instanceParam, levelParam);

                Type loggingEventType = Type.GetType("log4net.Core.LoggingEvent, log4net");

                _createLoggingEvent = GetCreateLoggingEvent(instanceParam, instanceCast, levelParam, levelCast, loggingEventType);

                _logDelegate = GetLogDelegate(loggerType, loggingEventType, instanceCast, instanceParam);

                _loggingEventPropertySetter = GetLoggingEventPropertySetter(loggingEventType);
            }

            [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "ILogger")]
            internal Log4NetLogger(object logger)
            {
                _logger = logger.GetType().GetRuntimeProperty("Logger").GetValue(logger, null);
            }

            private static Action<object, object> GetLogDelegate(Type loggerType, Type loggingEventType, UnaryExpression instanceCast,
                                                 ParameterExpression instanceParam)
            {
                //Action<object, object, string, Exception> Log =
                //(logger, callerStackBoundaryDeclaringType, level, message, exception) => { ((ILogger)logger).Log(new LoggingEvent(callerStackBoundaryDeclaringType, logger.Repository, logger.Name, level, message, exception)); }
                MethodInfo writeExceptionMethodInfo = loggerType.GetMethod("Log", new[] { loggingEventType });

                ParameterExpression loggingEventParameter =
                    Expression.Parameter(typeof(object), "loggingEvent");

                UnaryExpression loggingEventCasted =
                    Expression.Convert(loggingEventParameter, loggingEventType);

                var writeMethodExp = Expression.Call(
                    instanceCast,
                    writeExceptionMethodInfo,
                    loggingEventCasted);

                var logDelegate = Expression.Lambda<Action<object, object>>(
                                                writeMethodExp,
                                                instanceParam,
                                                loggingEventParameter).Compile();

                return logDelegate;
            }

            private static Func<object, Type, object, string, Exception, object> GetCreateLoggingEvent(ParameterExpression instanceParam, UnaryExpression instanceCast, ParameterExpression levelParam, UnaryExpression levelCast, Type loggingEventType)
            {
                ParameterExpression callerStackBoundaryDeclaringTypeParam = Expression.Parameter(typeof(Type));
                ParameterExpression messageParam = Expression.Parameter(typeof(string));
                ParameterExpression exceptionParam = Expression.Parameter(typeof(Exception));

                PropertyInfo repositoryProperty = loggingEventType.GetProperty("Repository");
                PropertyInfo levelProperty = loggingEventType.GetProperty("Level");

                ConstructorInfo loggingEventConstructor =
                    loggingEventType.GetConstructor(new Type[] {
                    typeof(Type),
                    repositoryProperty.PropertyType,
                    typeof(string),
                    levelProperty.PropertyType,
                    typeof(object),
                    typeof(Exception)
                });

                //Func<object, object, string, Exception, object> Log =
                //(logger, callerStackBoundaryDeclaringType, level, message, exception) => new LoggingEvent(callerStackBoundaryDeclaringType, ((ILogger)logger).Repository, ((ILogger)logger).Name, (Level)level, message, exception); }
                NewExpression newLoggingEventExpression =
                    Expression.New(loggingEventConstructor,
                                   callerStackBoundaryDeclaringTypeParam,
                                   Expression.Property(instanceCast, "Repository"),
                                   Expression.Property(instanceCast, "Name"),
                                   levelCast,
                                   messageParam,
                                   exceptionParam);

                var createLoggingEvent =
                    Expression.Lambda<Func<object, Type, object, string, Exception, object>>(
                                  newLoggingEventExpression,
                                  instanceParam,
                                  callerStackBoundaryDeclaringTypeParam,
                                  levelParam,
                                  messageParam,
                                  exceptionParam)
                              .Compile();

                return createLoggingEvent;
            }

            private static Func<object, object, bool> GetIsEnabledFor(Type loggerType, Type logEventLevelType,
                                                                      UnaryExpression instanceCast,
                                                                      UnaryExpression levelCast,
                                                                      ParameterExpression instanceParam,
                                                                      ParameterExpression levelParam)
            {
                MethodInfo isEnabledMethodInfo = loggerType.GetMethod("IsEnabledFor", new[] { logEventLevelType });
                MethodCallExpression isEnabledMethodCall = Expression.Call(instanceCast, isEnabledMethodInfo, levelCast);

                Func<object, object, bool> result =
                    Expression.Lambda<Func<object, object, bool>>(isEnabledMethodCall, instanceParam, levelParam)
                              .Compile();

                return result;
            }

            private static Action<object, string, object> GetLoggingEventPropertySetter(Type loggingEventType)
            {
                ParameterExpression loggingEventParameter = Expression.Parameter(typeof(object), "loggingEvent");
                ParameterExpression keyParameter = Expression.Parameter(typeof(string), "key");
                ParameterExpression valueParameter = Expression.Parameter(typeof(object), "value");

                PropertyInfo propertiesProperty = loggingEventType.GetProperty("Properties");
                PropertyInfo item = propertiesProperty.PropertyType.GetProperty("Item");

                // ((LoggingEvent)loggingEvent).Properties[key] = value;
                var body =
                    Expression.Assign(
                        Expression.Property(
                            Expression.Property(Expression.Convert(loggingEventParameter, loggingEventType),
                                                propertiesProperty), item, keyParameter), valueParameter);

                Action<object, string, object> result =
                    Expression.Lambda<Action<object, string, object>>
                              (body, loggingEventParameter, keyParameter,
                               valueParameter)
                              .Compile();

                return result;
            }

            public bool Log(TcLogLevel logLevel, Func<string> messageFunc, Exception exception, params object[] formatParameters)
            {
                if (messageFunc == null)
                {
                    return IsLogLevelEnable(logLevel);
                }

                if (!IsLogLevelEnable(logLevel))
                {
                    return false;
                }

                string message = messageFunc();

                IEnumerable<string> patternMatches;

                string formattedMessage =
                    LogMessageFormatter.FormatStructuredMessage(message,
                                                                formatParameters,
                                                                out patternMatches);

                // determine correct caller - this might change due to jit optimizations with method inlining
                if (s_callerStackBoundaryType == null)
                {
                    lock (CallerStackBoundaryTypeSync)
                        s_callerStackBoundaryType = typeof(TcLog);
                }

                var translatedLevel = TranslateLevel(logLevel);

                object loggingEvent = _createLoggingEvent(_logger, s_callerStackBoundaryType, translatedLevel, formattedMessage, exception);

                PopulateProperties(loggingEvent, patternMatches, formatParameters);

                _logDelegate(_logger, loggingEvent);

                return true;
            }

            private void PopulateProperties(object loggingEvent, IEnumerable<string> patternMatches, object[] formatParameters)
            {
                IEnumerable<KeyValuePair<string, object>> keyToValue =
                    patternMatches.Zip(formatParameters,
                                       (key, value) => new KeyValuePair<string, object>(key, value));

                foreach (KeyValuePair<string, object> keyValuePair in keyToValue)
                {
                    _loggingEventPropertySetter(loggingEvent, keyValuePair.Key, keyValuePair.Value);
                }
            }

            private static bool IsInTypeHierarchy(Type currentType, Type checkType)
            {
                while (currentType != null && currentType != typeof(object))
                {
                    if (currentType == checkType) return true;

                    currentType = currentType.BaseType;
                }
                return false;
            }

            private bool IsLogLevelEnable(TcLogLevel logLevel)
            {
                var level = TranslateLevel(logLevel);
                return _isEnabledForDelegate(_logger, level);
            }

            private object TranslateLevel(TcLogLevel logLevel)
            {
                switch (logLevel)
                {
                    case TcLogLevel.Trace:
                    case TcLogLevel.Debug:
                        return _levelDebug;

                    case TcLogLevel.Info:
                        return _levelInfo;

                    case TcLogLevel.Warn:
                        return _levelWarn;

                    case TcLogLevel.Error:
                        return _levelError;

                    case TcLogLevel.Fatal:
                        return _levelFatal;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
                }
            }
        }
    }

    internal class EntLibLogProvider : TcBaseLogProvider
    {
        private const string TypeTemplate = "Microsoft.Practices.EnterpriseLibrary.Logging.{0}, Microsoft.Practices.EnterpriseLibrary.Logging";
        private static readonly Type LogEntryType;
        private static readonly Type LoggerType;
        private static readonly Action<string, string, TraceEventType> WriteLogEntry;
        private static readonly Func<string, TraceEventType, bool> ShouldLogEntry;

        static EntLibLogProvider()
        {
            LogEntryType = Type.GetType(string.Format(CultureInfo.InvariantCulture, TypeTemplate, "LogEntry"));
            LoggerType = Type.GetType(string.Format(CultureInfo.InvariantCulture, TypeTemplate, "Logger"));
            if (LogEntryType == null || LoggerType == null) return;

            WriteLogEntry = GetWriteLogEntry();
            ShouldLogEntry = GetShouldLogEntry();
        }

        public EntLibLogProvider()
        {
            if (!IsLoggerAvailable())
            {
                throw new InvalidOperationException("Microsoft.Practices.EnterpriseLibrary.Logging.Logger not found");
            }
        }

        protected override Logger GetLogger(string name)
            => new EntLibLogger(name, WriteLogEntry, ShouldLogEntry).Log;

        internal static bool IsLoggerAvailable() => LogEntryType != null;

        private static Action<string, string, TraceEventType> GetWriteLogEntry()
        {
            // new LogEntry(...)
            var logNameParameter = Expression.Parameter(typeof(string), "logName");
            var messageParameter = Expression.Parameter(typeof(string), "message");
            var severityParameter = Expression.Parameter(typeof(TraceEventType), "severity");

            MemberInitExpression memberInit = GetWriteLogExpression(
                messageParameter,
                severityParameter,
                logNameParameter);

            //Logger.Write(new LogEntry(....));
            MethodInfo writeLogEntryMethod = LoggerType.GetMethod("Write", new[] { LogEntryType });
            var writeLogEntryExpression = Expression.Call(writeLogEntryMethod, memberInit);

            return Expression.Lambda<Action<string, string, TraceEventType>>(
                writeLogEntryExpression,
                logNameParameter,
                messageParameter,
                severityParameter).Compile();
        }

        private static Func<string, TraceEventType, bool> GetShouldLogEntry()
        {
            // new LogEntry(...)
            var logNameParameter = Expression.Parameter(typeof(string), "logName");
            var severityParameter = Expression.Parameter(typeof(TraceEventType), "severity");

            MemberInitExpression memberInit = GetWriteLogExpression(
                Expression.Constant("***dummy***"),
                severityParameter,
                logNameParameter);

            //Logger.Write(new LogEntry(....));
            MethodInfo writeLogEntryMethod = LoggerType.GetMethod("ShouldLog", new[] { LogEntryType });
            var writeLogEntryExpression = Expression.Call(writeLogEntryMethod, memberInit);

            return Expression.Lambda<Func<string, TraceEventType, bool>>(
                writeLogEntryExpression,
                logNameParameter,
                severityParameter).Compile();
        }

        private static MemberInitExpression GetWriteLogExpression(Expression message,
            Expression severityParameter, ParameterExpression logNameParameter)
        {
            var entryType = LogEntryType;
            MemberInitExpression memberInit = Expression.MemberInit(Expression.New(entryType),
                Expression.Bind(entryType.GetProperty("Message"), message),
                Expression.Bind(entryType.GetProperty("Severity"), severityParameter),
                Expression.Bind(
                    entryType.GetProperty("TimeStamp"),
                    Expression.Property(null, typeof(DateTime).GetProperty("UtcNow"))),
                Expression.Bind(
                    entryType.GetProperty("Categories"),
                    Expression.ListInit(
                        Expression.New(typeof(List<string>)),
                        typeof(List<string>).GetMethod("Add", new[] { typeof(string) }),
                        logNameParameter)));
            return memberInit;
        }

        internal class EntLibLogger
        {
            private readonly string _loggerName;
            private readonly Action<string, string, TraceEventType> _writeLog;
            private readonly Func<string, TraceEventType, bool> _shouldLog;

            internal EntLibLogger(string loggerName, Action<string, string, TraceEventType> writeLog, Func<string, TraceEventType, bool> shouldLog)
            {
                _loggerName = loggerName;
                _writeLog = writeLog;
                _shouldLog = shouldLog;
            }

            public bool Log(TcLogLevel logLevel, Func<string> messageFunc, Exception exception, params object[] formatParameters)
            {
                var severity = MapSeverity(logLevel);
                if (messageFunc == null) return _shouldLog(_loggerName, severity);

                messageFunc = LogMessageFormatter.SimulateStructuredLogging(messageFunc, formatParameters);
                if (exception != null) return LogException(logLevel, messageFunc, exception);

                _writeLog(_loggerName, messageFunc(), severity);
                return true;
            }

            public bool LogException(TcLogLevel logLevel, Func<string> messageFunc, Exception exception)
            {
                var severity = MapSeverity(logLevel);
                var message = messageFunc() + Environment.NewLine + exception;
                _writeLog(_loggerName, message, severity);
                return true;
            }

            private static TraceEventType MapSeverity(TcLogLevel logLevel)
            {
                switch (logLevel)
                {
                    case TcLogLevel.Fatal:
                        return TraceEventType.Critical;

                    case TcLogLevel.Error:
                        return TraceEventType.Error;

                    case TcLogLevel.Warn:
                        return TraceEventType.Warning;

                    case TcLogLevel.Info:
                        return TraceEventType.Information;

                    default:
                        return TraceEventType.Verbose;
                }
            }
        }
    }

    internal class SerilogLogProvider : TcBaseLogProvider
    {
        private readonly Func<string, object> _getLoggerByNameDelegate;
        private static Func<string, string, IDisposable> _pushProperty;

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "Serilog")]
        public SerilogLogProvider()
        {
            if (!IsLoggerAvailable())
            {
                throw new InvalidOperationException("Serilog.Log not found");
            }
            _getLoggerByNameDelegate = GetForContextMethodCall();
            _pushProperty = GetPushProperty();
        }

        protected override Logger GetLogger(string name)
            => new SerilogLogger(_getLoggerByNameDelegate(name)).Log;

        internal static bool IsLoggerAvailable()
            => GetLogManagerType() != null;

        protected override OpenNdc GetOpenNdcMethod()
            => message => _pushProperty("NDC", message);

        protected override OpenMdc GetOpenMdcMethod()
            => (key, value) => _pushProperty(key, value);

        private static Func<string, string, IDisposable> GetPushProperty()
        {
            Type ndcContextType = Type.GetType("Serilog.Context.LogContext, Serilog") ??
                                  Type.GetType("Serilog.Context.LogContext, Serilog.FullNetFx");

            MethodInfo pushPropertyMethod = ndcContextType.GetMethod("PushProperty", new[]
            {
                typeof(string),
                typeof(object),
                typeof(bool)
            });

            ParameterExpression nameParam = Expression.Parameter(typeof(string), "name");
            ParameterExpression valueParam = Expression.Parameter(typeof(object), "value");
            ParameterExpression destructureObjectParam = Expression.Parameter(typeof(bool), "destructureObjects");
            MethodCallExpression pushPropertyMethodCall = Expression
                .Call(null, pushPropertyMethod, nameParam, valueParam, destructureObjectParam);
            var pushProperty = Expression
                .Lambda<Func<string, object, bool, IDisposable>>(
                    pushPropertyMethodCall,
                    nameParam,
                    valueParam,
                    destructureObjectParam)
                .Compile();

            return (key, value) => pushProperty(key, value, false);
        }

        private static Type GetLogManagerType()
            => Type.GetType("Serilog.Log, Serilog");

        private static Func<string, object> GetForContextMethodCall()
        {
            Type logManagerType = GetLogManagerType();
            MethodInfo method = logManagerType.GetMethod("ForContext", new[] { typeof(string), typeof(object), typeof(bool) });
            ParameterExpression propertyNameParam = Expression.Parameter(typeof(string), "propertyName");
            ParameterExpression valueParam = Expression.Parameter(typeof(object), "value");
            ParameterExpression destructureObjectsParam = Expression.Parameter(typeof(bool), "destructureObjects");
            MethodCallExpression methodCall = Expression.Call(null, method, new Expression[]
            {
                propertyNameParam,
                valueParam,
                destructureObjectsParam
            });
            var func = Expression.Lambda<Func<string, object, bool, object>>(
                methodCall,
                propertyNameParam,
                valueParam,
                destructureObjectsParam)
                .Compile();
            return name => func("SourceContext", name, false);
        }

        internal class SerilogLogger
        {
            private readonly object _logger;
            private static readonly object DebugLevel;
            private static readonly object ErrorLevel;
            private static readonly object FatalLevel;
            private static readonly object InformationLevel;
            private static readonly object VerboseLevel;
            private static readonly object WarningLevel;
            private static readonly Func<object, object, bool> IsEnabled;
            private static readonly Action<object, object, string, object[]> Write;
            private static readonly Action<object, object, Exception, string, object[]> WriteException;

            [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
            [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
            [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "ILogger")]
            [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "LogEventLevel")]
            [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "Serilog")]
            static SerilogLogger()
            {
                var logEventLevelType = Type.GetType("Serilog.Events.LogEventLevel, Serilog");
                if (logEventLevelType == null)
                {
                    throw new InvalidOperationException("Type Serilog.Events.LogEventLevel was not found.");
                }
                DebugLevel = Enum.Parse(logEventLevelType, "Debug", false);
                ErrorLevel = Enum.Parse(logEventLevelType, "Error", false);
                FatalLevel = Enum.Parse(logEventLevelType, "Fatal", false);
                InformationLevel = Enum.Parse(logEventLevelType, "Information", false);
                VerboseLevel = Enum.Parse(logEventLevelType, "Verbose", false);
                WarningLevel = Enum.Parse(logEventLevelType, "Warning", false);

                // Func<object, object, bool> isEnabled = (logger, level) => { return ((SeriLog.ILogger)logger).IsEnabled(level); }
                var loggerType = Type.GetType("Serilog.ILogger, Serilog");
                if (loggerType == null)
                {
                    throw new InvalidOperationException("Type Serilog.ILogger was not found.");
                }
                MethodInfo isEnabledMethodInfo = loggerType.GetMethod("IsEnabled", new[] { logEventLevelType });
                ParameterExpression instanceParam = Expression.Parameter(typeof(object));
                UnaryExpression instanceCast = Expression.Convert(instanceParam, loggerType);
                ParameterExpression levelParam = Expression.Parameter(typeof(object));
                UnaryExpression levelCast = Expression.Convert(levelParam, logEventLevelType);
                MethodCallExpression isEnabledMethodCall = Expression.Call(instanceCast, isEnabledMethodInfo, levelCast);
                IsEnabled = Expression.Lambda<Func<object, object, bool>>(isEnabledMethodCall, instanceParam, levelParam).Compile();

                // Action<object, object, string> Write =
                // (logger, level, message, params) => { ((SeriLog.ILoggerILogger)logger).Write(level, message, params); }
                MethodInfo writeMethodInfo = loggerType.GetMethod("Write", new[]
                {
                    logEventLevelType,
                    typeof(string),
                    typeof(object[])
                });
                ParameterExpression messageParam = Expression.Parameter(typeof(string));
                ParameterExpression propertyValuesParam = Expression.Parameter(typeof(object[]));
                MethodCallExpression writeMethodExp = Expression.Call(
                    instanceCast,
                    writeMethodInfo,
                    levelCast,
                    messageParam,
                    propertyValuesParam);
                var expression = Expression.Lambda<Action<object, object, string, object[]>>(
                    writeMethodExp,
                    instanceParam,
                    levelParam,
                    messageParam,
                    propertyValuesParam);
                Write = expression.Compile();

                // Action<object, object, string, Exception> WriteException =
                // (logger, level, exception, message) => { ((ILogger)logger).Write(level, exception, message, new object[]); }
                MethodInfo writeExceptionMethodInfo = loggerType.GetMethod("Write", new[] {
                    logEventLevelType,
                    typeof(Exception),
                    typeof(string),
                    typeof(object[])
                });
                ParameterExpression exceptionParam = Expression.Parameter(typeof(Exception));
                writeMethodExp = Expression.Call(
                    instanceCast,
                    writeExceptionMethodInfo,
                    levelCast,
                    exceptionParam,
                    messageParam,
                    propertyValuesParam);
                WriteException = Expression.Lambda<Action<object, object, Exception, string, object[]>>(
                    writeMethodExp,
                    instanceParam,
                    levelParam,
                    exceptionParam,
                    messageParam,
                    propertyValuesParam).Compile();
            }

            internal SerilogLogger(object logger)
            {
                _logger = logger;
            }

            public bool Log(TcLogLevel logLevel, Func<string> messageFunc, Exception exception, params object[] formatParameters)
            {
                var translatedLevel = TranslateLevel(logLevel);
                if (messageFunc == null)
                {
                    return IsEnabled(_logger, translatedLevel);
                }

                if (!IsEnabled(_logger, translatedLevel))
                {
                    return false;
                }

                if (exception != null)
                {
                    LogException(translatedLevel, messageFunc, exception, formatParameters);
                }
                else
                {
                    LogMessage(translatedLevel, messageFunc, formatParameters);
                }

                return true;
            }

            private void LogMessage(object translatedLevel, Func<string> messageFunc, object[] formatParameters)
            {
                Write(_logger, translatedLevel, messageFunc(), formatParameters);
            }

            private void LogException(object logLevel, Func<string> messageFunc, Exception exception, object[] formatParams)
            {
                WriteException(_logger, logLevel, exception, messageFunc(), formatParams);
            }

            private static object TranslateLevel(TcLogLevel logLevel)
            {
                switch (logLevel)
                {
                    case TcLogLevel.Fatal:
                        return FatalLevel;

                    case TcLogLevel.Error:
                        return ErrorLevel;

                    case TcLogLevel.Warn:
                        return WarningLevel;

                    case TcLogLevel.Info:
                        return InformationLevel;

                    case TcLogLevel.Trace:
                        return VerboseLevel;

                    default:
                        return DebugLevel;
                }
            }
        }
    }

    internal class LoupeLogProvider : TcBaseLogProvider
    {
        /// <summary>
        /// The form of the Loupe Log.Write method we're using
        /// </summary>
        internal delegate void WriteDelegate(
            int severity,
            string logSystem,
            int skipFrames,
            Exception exception,
            bool attributeToException,
            int writeMode,
            string detailsXml,
            string category,
            string caption,
            string description,
            params object[] args
            );

        private readonly WriteDelegate _logWriteDelegate;

        public LoupeLogProvider()
        {
            if (!IsLoggerAvailable())
            {
                throw new InvalidOperationException("Gibraltar.Agent.Log (Loupe) not found");
            }

            _logWriteDelegate = GetLogWriteDelegate();
        }

        protected override Logger GetLogger(string name)
            => new LoupeLogger(name, _logWriteDelegate).Log;

        public static bool IsLoggerAvailable()
            => GetLogManagerType() != null;

        private static Type GetLogManagerType()
            => Type.GetType("Gibraltar.Agent.Log, Gibraltar.Agent");

        private static WriteDelegate GetLogWriteDelegate()
        {
            Type logManagerType = GetLogManagerType();
            Type logMessageSeverityType = Type.GetType("Gibraltar.Agent.LogMessageSeverity, Gibraltar.Agent");
            Type logWriteModeType = Type.GetType("Gibraltar.Agent.LogWriteMode, Gibraltar.Agent");

            MethodInfo method = logManagerType.GetMethod(
                "Write", new[] {
                logMessageSeverityType, typeof(string), typeof(int), typeof(Exception), typeof(bool),
                logWriteModeType, typeof(string), typeof(string), typeof(string), typeof(string), typeof(object[]) });

            var callDelegate = (WriteDelegate)method.CreateDelegate(typeof(WriteDelegate));
            return callDelegate;
        }

        internal class LoupeLogger
        {
            private const string LogSystem = "LibLog";

            private readonly string _category;
            private readonly WriteDelegate _logWriteDelegate;
            private readonly int _skipLevel;

            internal LoupeLogger(string category, WriteDelegate logWriteDelegate)
            {
                _category = category;
                _logWriteDelegate = logWriteDelegate;
#if DEBUG
                _skipLevel = 2;
#else
                _skipLevel = 1;
#endif
            }

            public bool Log(TcLogLevel logLevel, Func<string> messageFunc, Exception exception, params object[] formatParameters)
            {
                if (messageFunc == null)
                {
                    //nothing to log..
                    return true;
                }

                messageFunc = LogMessageFormatter.SimulateStructuredLogging(messageFunc, formatParameters);

                _logWriteDelegate((int)ToLogMessageSeverity(logLevel), LogSystem, _skipLevel, exception, true, 0, null,
                    _category, null, messageFunc.Invoke());

                return true;
            }

            private static TraceEventType ToLogMessageSeverity(TcLogLevel logLevel)
            {
                switch (logLevel)
                {
                    case TcLogLevel.Trace:
                        return TraceEventType.Verbose;

                    case TcLogLevel.Debug:
                        return TraceEventType.Verbose;

                    case TcLogLevel.Info:
                        return TraceEventType.Information;

                    case TcLogLevel.Warn:
                        return TraceEventType.Warning;

                    case TcLogLevel.Error:
                        return TraceEventType.Error;

                    case TcLogLevel.Fatal:
                        return TraceEventType.Critical;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(logLevel));
                }
            }
        }
    }
}