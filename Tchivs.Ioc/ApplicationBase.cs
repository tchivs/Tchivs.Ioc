﻿using Tchivs.Ioc.Logging;
namespace Tchivs.Ioc
{
    public abstract class ApplicationBase
    {
        #region properties

        protected ITcLog Logger { get; }

        #endregion

        #region constructors

        protected ApplicationBase(ITcLogProvider logProvider)
        {
            Logger = logProvider.GetLogFor<ApplicationBase>();
        }

        #endregion

        #region abstracct methods

        /// <summary>
        /// 启动应用程序
        /// </summary>
        /// <param name="arguments">参数</param>
        public abstract void Start(string[] arguments);

        /// <summary>
        /// 停止应用程序
        /// </summary>
        public abstract void Stop();

        #endregion
    }
}