namespace Tchivs.Ioc
{
    /// <summary>
    /// 应用程序入口点
    /// </summary>
    public abstract class AppSetupBase
    {
        private readonly IIoCProvider _ioCProvider;
        public static bool IsStart { get; protected set; }

        public virtual ApplicationBase StartApp()
        {
            if (IsStart)
            {
                return null;
            }
            IsStart = true;
            return _ioCProvider.Resolve<ApplicationBase>();
        }

        #region constructors

        protected AppSetupBase(IIoCProvider ioCProvider)
        {
            _ioCProvider = ioCProvider;
        }

        #endregion constructors
    }
}