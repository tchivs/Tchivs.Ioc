using System;

namespace Tchivs.Ioc
{
    public interface IIoCProvider
    {
        bool CanResolve<T>() where T : class;

        bool CanResolve(Type type);

        IIoCProvider CreateChildContainer();

        void ResolveScope(Type type, Action<Object> action = null);

        void ResolveScope<T>(Action<T> action = null);

        object Resolve(Type type);

        T Resolve<T>() where T : class;

        bool TryResolve(Type type, out object resolved);

        bool TryResolve<T>(out T resolved) where T : class;
    }
}