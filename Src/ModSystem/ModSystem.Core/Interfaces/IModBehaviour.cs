using System;
using System.Collections.Generic;
using System.Text;

namespace ModSystem.Core.Interfaces
{
    /// <summary>
    /// 模组行为的基础接口
    /// </summary>
    public interface IModBehaviour
    {
        /// <summary>
        /// 模组的唯一标识符
        /// </summary>
        string ModId { get; }

        /// <summary>
        /// 初始化模组
        /// </summary>
        void Initialize();

        /// <summary>
        /// 关闭模组
        /// </summary>
        void Shutdown();
    }
}
