using System;
using System.Collections.Generic;
using System.Text;

namespace ModSystem.Core
{
    /// <summary>
    /// 支持热重载的模组接口（可选）
    /// </summary>
    public interface IReloadable
    {
        /// <summary>
        /// 在重载前调用，用于保存状态
        /// </summary>
        void OnBeforeReload();

        /// <summary>
        /// 在重载后调用，用于恢复状态
        /// </summary>
        void OnAfterReload();
    }

}
