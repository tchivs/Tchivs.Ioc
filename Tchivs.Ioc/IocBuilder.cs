using System;
using Autofac;

namespace Tchivs.Ioc
{

    /// <summary>
    /// 应用程序入口点
    /// </summary>
    public abstract class AppSetup
    {
        public static bool IsStart { get; protected set; } 

        public virtual void StartApp()
        {
            if (IsStart)
            {
                return;
            }
            IsStart = true;
        }

        /// <summary>
        /// 应用程序入口
        /// </summary>
        /// <param name="registerAction">如果需要额外注册事件则传入该方法</param>
        /// <returns></returns>
        public static AppSetup Start<TIiSetup>(Action<IIoCProvider> registerAction) where TIiSetup : AppSetup
        {
            var ioc = IocBuilder.Create()
                  .RegisterSetup<TIiSetup>()
                  .Build();
            registerAction?.Invoke(ioc);
            return ioc.Resolve<AppSetup>();
        }
    }

    public class IocBuilder : IIocBuilder
    {
        private readonly ContainerBuilder _builder;

        #region constructors

        public IocBuilder()
        {
            _builder = new ContainerBuilder();
        }
        #endregion

        #region override


        public IIocBuilder RegisterSingleton(Type tInterface, Func<object> theConstructor)
        {
            _builder.Register(c => theConstructor).As(tInterface).SingleInstance();
            return this;
        }




        public IIocBuilder RegisterSingleton<TInterface>(TInterface theObject) where TInterface : class
        {
            _builder.RegisterInstance(theObject).As<TInterface>().SingleInstance();
            return this;

        }

        public IIocBuilder RegisterSingleton<TInterface, TType>() where TInterface : class where TType : class, TInterface
        {
            _builder.RegisterType<TType>().As<TInterface>().SingleInstance();
            return this;
        }

        public IIocBuilder RegisterSingleton<TInterface>(Func<TInterface> theConstructor) where TInterface : class
        {
            _builder.Register(c => theConstructor).As<TInterface>().SingleInstance();
            return this;
        }

        public IIocBuilder RegisterSingleton(Type tInterface, object theObject)
        {
            _builder.Register(c => theObject).As(tInterface).SingleInstance();
            return this;
        }

        public IIocBuilder RegisterType(Type tFrom, Type tTo)
        {
            _builder.RegisterType(tTo).As(tFrom);
            return this;
        }

        public IIocBuilder RegisterType(Type t, Func<object> constructor)
        {
            _builder.Register(c => constructor).As(t);
            return this;
        }

        public IIocBuilder RegisterType<TInterface>(Func<TInterface> constructor) where TInterface : class
        {
            _builder.Register(c => constructor).As<TInterface>();
            return this;
        }

        public IIocBuilder RegisterType<TFrom, TTo>() where TFrom : class where TTo : class, TFrom
        {
            _builder.RegisterType<TTo>().As<TFrom>();
            return this;
        }

        public IIoCProvider Build()
        {
            var i = new IoCProvider(_builder);
            return i;
        }

        #endregion

        public static IocBuilder Create()
        {
            IocBuilder iocBuilder = new IocBuilder();
            return iocBuilder;
        }
    }
}