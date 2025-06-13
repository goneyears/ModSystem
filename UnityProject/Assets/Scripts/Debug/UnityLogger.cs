// UnityProject/Assets/Scripts/UnityImplementations/UnityLogger.cs
using UnityEngine;
using ModSystem.Core;

namespace ModSystem.Unity
{
    /// <summary>
    /// Unity日志实现
    /// </summary>
    public class UnityLogger : ILogger
    {
        private readonly string prefix;
        private readonly bool includeTimestamp;
        
        /// <summary>
        /// 创建Unity日志记录器
        /// </summary>
        /// <param name="prefix">日志前缀</param>
        /// <param name="includeTimestamp">是否包含时间戳</param>
        public UnityLogger(string prefix = "", bool includeTimestamp = false)
        {
            this.prefix = prefix;
            this.includeTimestamp = includeTimestamp;
        }
        
        /// <summary>
        /// 记录普通日志
        /// </summary>
        public void Log(string message)
        {
            Debug.Log(FormatMessage(message));
        }
        
        /// <summary>
        /// 记录警告日志
        /// </summary>
        public void LogWarning(string message)
        {
            Debug.LogWarning(FormatMessage(message));
        }
        
        /// <summary>
        /// 记录错误日志
        /// </summary>
        public void LogError(string message)
        {
            Debug.LogError(FormatMessage(message));
        }
        
        /// <summary>
        /// 格式化消息
        /// </summary>
        private string FormatMessage(string message)
        {
            if (includeTimestamp)
            {
                return $"[{System.DateTime.Now:HH:mm:ss.fff}] {prefix}{message}";
            }
            return $"{prefix}{message}";
        }
    }
    
}