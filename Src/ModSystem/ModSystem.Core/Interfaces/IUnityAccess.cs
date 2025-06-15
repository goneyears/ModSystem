using System;

namespace ModSystem.Core.Interfaces
{
    /// <summary>
    /// Unity访问接口 - 简化版，只提供必要的桥接
    /// </summary>
    public interface IUnityAccess
    {
        /// <summary>
        /// 检查是否在Unity环境中运行
        /// </summary>
        bool IsUnityEnvironment { get; }

        /// <summary>
        /// 获取Unity程序集（用于反射）
        /// </summary>
        System.Reflection.Assembly[] GetUnityAssemblies();
    }
}