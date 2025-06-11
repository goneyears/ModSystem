// ModSystem.Unity/UnityImplementations/UnityLogger.cs
using UnityEngine;
using ModSystem.Core;
using System;

namespace ModSystem.Unity
{
    /// <summary>
    /// Unity日志记录器实现
    /// 将Core层的日志调用转发到Unity的Debug类
    /// </summary>
    public class UnityLogger : ILogger
    {
        #region Fields
        private readonly string prefix;
        private readonly bool includeTimestamp;
        private readonly LogLevel minLogLevel;
        #endregion

        #region Enums
        /// <summary>
        /// 日志级别枚举
        /// </summary>
        public enum LogLevel
        {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3
        }
        #endregion

        #region Constructor
        /// <summary>
        /// 创建Unity日志记录器
        /// </summary>
        /// <param name="prefix">日志前缀</param>
        /// <param name="includeTimestamp">是否包含时间戳</param>
        /// <param name="minLogLevel">最小日志级别</param>
        public UnityLogger(string prefix = "[ModSystem]", bool includeTimestamp = false, LogLevel minLogLevel = LogLevel.Info)
        {
            this.prefix = prefix;
            this.includeTimestamp = includeTimestamp;
            this.minLogLevel = minLogLevel;
        }
        #endregion

        #region ILogger Implementation
        /// <summary>
        /// 记录普通日志
        /// </summary>
        public void Log(string message)
        {
            if (minLogLevel <= LogLevel.Info)
            {
                Debug.Log(FormatMessage(message, LogLevel.Info));
            }
        }
        
        /// <summary>
        /// 记录警告日志
        /// </summary>
        public void LogWarning(string message)
        {
            if (minLogLevel <= LogLevel.Warning)
            {
                Debug.LogWarning(FormatMessage(message, LogLevel.Warning));
            }
        }
        
        /// <summary>
        /// 记录错误日志
        /// </summary>
        public void LogError(string message)
        {
            if (minLogLevel <= LogLevel.Error)
            {
                Debug.LogError(FormatMessage(message, LogLevel.Error));
            }
        }
        #endregion

        #region Extended Logging Methods
        /// <summary>
        /// 记录调试日志
        /// </summary>
        public void LogDebug(string message)
        {
            if (minLogLevel <= LogLevel.Debug)
            {
                Debug.Log(FormatMessage(message, LogLevel.Debug));
            }
        }
        
        /// <summary>
        /// 记录异常
        /// </summary>
        public void LogException(Exception exception, string context = null)
        {
            string message = string.IsNullOrEmpty(context) 
                ? $"Exception: {exception.Message}" 
                : $"Exception in {context}: {exception.Message}";
            
            Debug.LogError(FormatMessage(message, LogLevel.Error));
            Debug.LogException(exception);
        }
        
        /// <summary>
        /// 记录断言失败
        /// </summary>
        public void LogAssertion(string condition, string message)
        {
            Debug.LogAssertion(FormatMessage($"Assertion failed: {condition} - {message}", LogLevel.Error));
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// 格式化日志消息
        /// </summary>
        private string FormatMessage(string message, LogLevel level)
        {
            var formattedMessage = message;
            
            // 添加时间戳
            if (includeTimestamp)
            {
                formattedMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {formattedMessage}";
            }
            
            // 添加日志级别
            if (level == LogLevel.Debug)
            {
                formattedMessage = $"[DEBUG] {formattedMessage}";
            }
            
            // 添加前缀
            if (!string.IsNullOrEmpty(prefix))
            {
                formattedMessage = $"{prefix} {formattedMessage}";
            }
            
            return formattedMessage;
        }
        #endregion

        #region Conditional Logging
        /// <summary>
        /// 条件日志记录
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void LogEditor(string message)
        {
            Log($"[EDITOR] {message}");
        }
        
        /// <summary>
        /// 开发构建日志记录
        /// </summary>
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public void LogDevelopment(string message)
        {
            Log($"[DEV] {message}");
        }
        #endregion
    }
} 