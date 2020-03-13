using System;
using Tchivs.Ioc;
using Tchivs.Ioc.Logging;

namespace Ioc.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var ioc = IocBuilder.Create()
                .RegisterType<ITestService, TestServiceImpl>()
                .UseLogProvider(MvxLogProviderType.NLog)
                .Build();
            Console.WriteLine("Hello World!");
            // var testService = ioc.Resolve<ITestService>();
            ioc.ResolveScope<ITestService>(x =>
            {
                while (true)
                {
                    var log = ioc.Resolve<ITcLogProvider>();
                    var logger = log.GetLogFor<Program>();
                    logger.Info(x.GetIndex().ToString());
                    Console.WriteLine(x.GetIndex());
                    var k = Console.ReadLine();
                    if (k == "a")
                    {
                        break;
                    }
                }

            });

            var service = ioc.Resolve<ITestService>();
            while (true)
            {
                Console.WriteLine(service.GetIndex());
                Console.ReadKey();
            }

        }
    }


}
