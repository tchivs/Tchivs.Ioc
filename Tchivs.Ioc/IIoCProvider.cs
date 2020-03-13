using System;
using System.Collections.Generic;

namespace Tchivs.Ioc
{
    public interface IIoCProvider
    {
        //AppSetup AppSetup { get; }
        bool CanResolve<T>() where T : class;
        bool CanResolve(Type type);
        //object Create(Type type);
        //T Create<T>() where T : class;
        IIoCProvider CreateChildContainer();

        void ResolveScope(Type type, Action<Object> action);
        void ResolveScope<T>(Action<T> action);
        object Resolve(Type type);
        T Resolve<T>() where T : class;
        bool TryResolve(Type type, out object resolved);
        bool TryResolve<T>(out T resolved) where T : class;
    }
}