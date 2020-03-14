﻿using System;
using Autofac;
using Tchivs.Ioc.Logging;

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
    }

    public class IocBuilder : IIocBuilder
    {
        #region fields

        private readonly ContainerBuilder _builder;

        #endregion

        #region constructors

        public IocBuilder()
        {
            _builder = new ContainerBuilder();
            this.UseLogProvider();
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
           // _builder.Register(x => theConstructor).As<TInterface>().SingleInstance();
            _builder.RegisterInstance(theConstructor()).As<TInterface>().SingleInstance();
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

        #region functhon

        public static IocBuilder Create()
        {
            IocBuilder iocBuilder = new IocBuilder();
            return iocBuilder;
        }

        #endregion
    }
}