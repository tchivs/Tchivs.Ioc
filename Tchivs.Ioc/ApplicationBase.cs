using Tchivs.Ioc.Logging;
namespace Tchivs.Ioc
{
    public abstract class ApplicationBase
    {
        protected ITcLog Logger { get; set; }

        protected ApplicationBase(ITcLogProvider logProvider)
        {
            Logger = logProvider.GetLogFor<ApplicationBase>();
        }
        /// <summary>
        /// 启动应用程序
        /// </summary>
        /// <param name="arguments">参数</param>
        public abstract void Start(string[] arguments);
        public abstract void Stop();
    }
}