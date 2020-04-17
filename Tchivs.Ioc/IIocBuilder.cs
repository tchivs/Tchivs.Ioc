using System;

namespace Tchivs.Ioc
{
    public interface IIocBuilder
    {
        IIocBuilder RegisterSingleton(Type tInterface, Func<object> theConstructor);

        IIocBuilder RegisterSingleton<TInterface>(TInterface theObject) where TInterface : class;

        IIocBuilder RegisterSingleton<TInterface, TType>() where TInterface : class where TType : class, TInterface;

        IIocBuilder RegisterSingleton<TInterface>(Func<TInterface> theConstructor) where TInterface : class;

        IIocBuilder RegisterSingleton(Type tInterface, object theObject);

        IIocBuilder RegisterType(Type tFrom, Type tTo);

        IIocBuilder RegisterType(Type t, Func<object> constructor);

        IIocBuilder RegisterType<TInterface>(Func<TInterface> constructor) where TInterface : class;

        IIocBuilder RegisterType<TFrom, TTo>()
            where TFrom : class
            where TTo : class, TFrom;

        IIoCProvider Build();
    }
}