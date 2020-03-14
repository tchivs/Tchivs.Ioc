using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Builder;

namespace Tchivs.Ioc
{
    public class IoCProvider : IIoCProvider
    {
        private static IContainer _container;
        private ContainerBuilder _builder;
        // private IIoCProvider _ioC;
        public static IIoCProvider Provider { get; private set; } //_container.Resolve<IIoCProvider>();
                                                                  //public AppSetupBase AppSetupBase => _container.Resolve<AppSetupBase>();

        internal IoCProvider(ContainerBuilder builder)
        {
            if (Provider == null)
            {
                Provider = this;
            }
            _builder = builder;
            builder.RegisterInstance(this).As<IIoCProvider>();
            builder.Register<IContainer>((x, c) => GetContainer());
            _container = builder.Build();
        }

        void SetupScope(Action<AppSetupBase> action)
        {
            using (var scope = _container.BeginLifetimeScope())
            {
                var o = scope.Resolve<AppSetupBase>();
                action.Invoke(o);
            }
        }

        IContainer GetContainer()
        {
            return _container;
        }


        public bool CanResolve<T>() where T : class
        {
            return _container.IsRegistered<T>();

        }

        public bool CanResolve(Type type)
        {
            return _container.IsRegistered(type);
        }


        public IIoCProvider CreateChildContainer()
        {
            //_builder.ComponentRegistryBuilder.
            return new IoCProvider(new ContainerBuilder());
        }
        public void ResolveScope<T>(Action<T> action=null)
        {
            using (var scope = _container.BeginLifetimeScope())
            {
                var o = scope.Resolve<T>();
                action?.Invoke(o);
            }
        }
        public void ResolveScope(Type type, Action<Object> action=null)
        {
            using (var scope = _container.BeginLifetimeScope())
            {
                var o = scope.Resolve(type);
                action?.Invoke(o);
            }
        }

        public object Resolve(Type type)
        {
            return _container.Resolve(type);
        }

        public T Resolve<T>() where T : class
        {
            return _container.Resolve<T>();
        }

        public bool TryResolve(Type type, out object resolved)
        {
            return _container.TryResolve(type, out resolved);
        }

        public bool TryResolve<T>(out T resolved) where T : class
        {
            return _container.TryResolve<T>(out resolved);
        }
    }


    public interface ITestService
    {
        int index { get; }
        int GetIndex();

    }

    public class TestServiceImpl : ITestService
    {
        private int _index = 0;
        public int index => _index;

        public TestServiceImpl(IIoCProvider ioCProvider)
        {
            Console.WriteLine("create TestServiceImpl");
        }
        public int GetIndex()
        {
            _index++;
            return index;
        }
    }

}
