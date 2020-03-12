using System;
using Tchivs.Ioc;
namespace Ioc.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var ioc = IocBuilder.Create()
                .RegisterType<ITestService, TestServiceImpl>()
                .Build();
            Console.WriteLine("Hello World!");
            // var testService = ioc.Resolve<ITestService>();
            ioc.ResolveScope<ITestService>(x =>
            {
                while (true)
                {
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
