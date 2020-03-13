namespace Tchivs.Ioc
{
    public static class IocBuilderExtensions
    {
        public static IIocBuilder RegisterSetup<TIiSetup>(this IIocBuilder builder) where TIiSetup : AppSetup
        {
            builder.RegisterSingleton<AppSetup, TIiSetup>();
            return builder;
        }
    }
}