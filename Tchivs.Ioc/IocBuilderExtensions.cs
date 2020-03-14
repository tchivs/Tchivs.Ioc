using System;
using Tchivs.Ioc.Logging;
using Tchivs.Ioc.Logging.LogProviders;

namespace Tchivs.Ioc
{
    public static class IocBuilderExtensions
    {
        /// <summary>
        /// 日志构造器
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IIocBuilder UseLogProvider(this IIocBuilder builder, MvxLogProviderType type = MvxLogProviderType.Console)
        {
            Func<ITcLogProvider> logProviderCreator;
            switch (type)
            {
                case MvxLogProviderType.Console:
                    logProviderCreator = () => new ConsoleLogProvider();
                    break;

                case MvxLogProviderType.EntLib:
                    logProviderCreator = () => new EntLibLogProvider();
                    break;

                case MvxLogProviderType.Log4Net:
                    logProviderCreator = () => new Log4NetLogProvider();
                    break;

                case MvxLogProviderType.Loupe:
                    logProviderCreator = () => new LoupeLogProvider();
                    break;

                case MvxLogProviderType.NLog:
                    logProviderCreator = () => new NLogLogProvider();
                    break;

                case MvxLogProviderType.Serilog:
                    logProviderCreator = () => new SerilogLogProvider();
                    break;

                default:
                    logProviderCreator = null;
                    break;
            }
            if (logProviderCreator != null)
            {
                builder.RegisterSingleton<ITcLogProvider>(logProviderCreator);
            }
            return builder;
        }

        public static IIocBuilder UseApplication<TApplication>(this IIocBuilder builder) where TApplication : ApplicationBase
        {
            builder.RegisterSingleton<ApplicationBase, TApplication>();
            return builder;
        }

        public static AppSetup RegisterAndGetAppSetup<TAppSetup>(this IIocBuilder builder, Action<IIocBuilder> registerAction = null) where TAppSetup : AppSetup
        {
            builder.RegisterSetup<TAppSetup>();
            registerAction?.Invoke(builder);
            var ioc = builder.Build();
            return ioc.Resolve<AppSetup>();
        }


        public static IIocBuilder RegisterSetup<TIiSetup>(this IIocBuilder builder) where TIiSetup : AppSetup
        {
            builder.RegisterSingleton<AppSetup, TIiSetup>();
            return builder;
        }
    }





}