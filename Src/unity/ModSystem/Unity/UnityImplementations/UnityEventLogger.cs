// ModSystem.Unity/UnityImplementations/UnityEventLogger.cs
using UnityEngine;
using ModSystem.Core;
using System;

namespace ModSystem.Unity
{
    /// <summary>
    /// Unity事件日志记录器实现
    /// 记录系统中所有事件的执行情况
    /// </summary>
    public class UnityEventLogger : IEventLogger
    {
        #region Fields
        private readonly UnityLogger logger;
        #endregion

        #region Constructor
        /// <summary>
        /// 创建Unity事件日志记录器
        /// </summary>
        public UnityEventLogger()
        {
            logger = new UnityLogger("[EventBus]");
        }
        #endregion

        #region IEventLogger Implementation
        /// <summary>
        /// 记录事件发布
        /// </summary>
        public void LogEventPublished<T>(T eventData) where T : IModEvent
        {
            if (eventData == null)
            {
                logger.LogWarning("Attempted to publish null event");
                return;
            }

            logger.LogDebug($"Event published: {eventData.EventId} from {eventData.SenderId} at {eventData.Timestamp}");
        }
        
        /// <summary>
        /// 记录事件处理
        /// </summary>
        public void LogEventHandled<T>(T eventData, object handler, bool success) where T : IModEvent
        {
            if (eventData == null)
            {
                logger.LogWarning("Attempted to handle null event");
                return;
            }

            if (success)
            {
                logger.LogDebug($"Event handled: {eventData.EventId} by {handler?.GetType().Name ?? "Unknown"}");
            }
            else
            {
                logger.LogWarning($"Event handling failed: {eventData.EventId} by {handler?.GetType().Name ?? "Unknown"}");
            }
        }
        
        /// <summary>
        /// 记录事件订阅
        /// </summary>
        public void LogSubscription<T>(object subscriber, bool added) where T : IModEvent
        {
            string action = added ? "subscribed to" : "unsubscribed from";
            logger.LogDebug($"{subscriber?.GetType().Name ?? "Unknown"} {action} event type {typeof(T).Name}");
        }
        
        /// <summary>
        /// 记录事件错误
        /// </summary>
        public void LogError(string message, Exception exception = null)
        {
            if (exception != null)
            {
                logger.LogException(exception, message);
            }
            else
            {
                logger.LogError(message);
            }
        }
        #endregion
    }
} 