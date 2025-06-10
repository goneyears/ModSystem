using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 日志接口，用于替代Unity的Debug类
    /// 允许在不同平台实现自定义日志逻辑
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// 记录普通日志信息
        /// </summary>
        /// <param name="message">日志消息</param>
        void Log(string message);
        
        /// <summary>
        /// 记录警告信息
        /// </summary>
        /// <param name="message">警告消息</param>
        void LogWarning(string message);
        
        /// <summary>
        /// 记录错误信息
        /// </summary>
        /// <param name="message">错误消息</param>
        void LogError(string message);
    }
} 