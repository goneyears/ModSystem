using System;
using System.Collections.Generic;
using System.Text;

namespace ModSystem.Core
{
    /// <summary>
    /// 简化的模组接口 - 支持纯反射模组
    /// 所有基于反射的模组必须实现此接口
    /// </summary>
    public interface IMod
    {
        /// <summary>
        /// 初始化模组
        /// </summary>
        /// <param name="context">模组上下文</param>
        void OnInitialize(IModContext context);

        /// <summary>
        /// 启用模组
        /// </summary>
        void OnEnable();

        /// <summary>
        /// 禁用模组
        /// </summary>
        void OnDisable();

        /// <summary>
        /// 销毁模组
        /// </summary>
        void OnDestroy();
    }
}
