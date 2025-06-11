using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 路径提供接口，用于替代Unity的Application类
    /// 提供平台无关的路径访问
    /// </summary>
    public interface IPathProvider
    {
        /// <summary>
        /// 获取模组存放路径
        /// </summary>
        /// <returns>模组目录路径</returns>
        string GetModsPath();
        
        /// <summary>
        /// 获取配置文件路径
        /// </summary>
        /// <returns>配置目录路径</returns>
        string GetConfigPath();
        
        /// <summary>
        /// 获取临时文件路径
        /// </summary>
        /// <returns>临时目录路径</returns>
        string GetTempPath();
        
        /// <summary>
        /// 获取持久化数据路径
        /// </summary>
        /// <returns>持久化数据目录路径</returns>
        string GetPersistentDataPath();
    }
} 