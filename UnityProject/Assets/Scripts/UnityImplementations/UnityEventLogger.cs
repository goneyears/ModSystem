// UnityProject/Assets/Scripts/UnityImplementations/UnityEventLogger.cs
using UnityEngine;
using ModSystem.Core;
using System;
using System.Collections.Generic;

namespace ModSystem.Unity
{
    /// <summary>
    /// Unity事件日志记录器实现
    /// 记录系统中所有事件的执行情况
    /// </summary>
    public class UnityEventLogger : IEventLogger
    {
        #region Fields
        private readonly string prefix;
        private readonly bool enableDebugLogging;
        private readonly bool logToFile;
        private readonly Queue<EventLogEntry> logHistory;
        private readonly int maxHistorySize;
        #endregion

        #region Constructor
        /// <summary>
        /// 创建Unity事件日志记录器
        /// </summary>
        /// <param name="enableDebugLogging">是否启用调试日志</param>
        /// <param name="logToFile">是否记录到文件</param>
        /// <param name="maxHistorySize">最大历史记录数</param>
        public UnityEventLogger(bool enableDebugLogging = false, bool logToFile = false, int maxHistorySize = 100)
        {
            this.prefix = "[EventBus] ";
            this.enableDebugLogging = enableDebugLogging;
            this.logToFile = logToFile;
            this.maxHistorySize = maxHistorySize;
            this.logHistory = new Queue<EventLogEntry>();
        }
        #endregion

        #region IEventLogger Implementation
        /// <summary>
        /// 记录事件
        /// </summary>
        public void LogEvent(IModEvent e)
        {
            if (e == null)
            {
                LogError("Attempted to log null event");
                return;
            }

            var message = $"Event: {e.GetType().Name} (ID: {e.EventId}) from {e.SenderId} at {e.Timestamp:HH:mm:ss.fff}";
            
            if (enableDebugLogging)
            {
                Debug.Log(prefix + message);
            }

            AddToHistory(new EventLogEntry
            {
                Timestamp = e.Timestamp,
                EventType = e.GetType().Name,
                EventId = e.EventId,
                SenderId = e.SenderId,
                Message = message
            });
        }

        /// <summary>
        /// 记录事件订阅
        /// </summary>
        public void LogSubscription(string eventType, string subscriber)
        {
            var message = $"Subscription: {subscriber} subscribed to {eventType}";
            
            if (enableDebugLogging)
            {
                Debug.Log(prefix + message);
            }

            AddToHistory(new EventLogEntry
            {
                Timestamp = DateTime.Now,
                EventType = "Subscription",
                EventId = eventType,
                SenderId = subscriber,
                Message = message
            });
        }

        /// <summary>
        /// 记录错误
        /// </summary>
        public void LogError(string message)
        {
            var errorMessage = prefix + "ERROR: " + message;
            Debug.LogError(errorMessage);

            AddToHistory(new EventLogEntry
            {
                Timestamp = DateTime.Now,
                EventType = "Error",
                EventId = "Error",
                SenderId = "System",
                Message = message,
                IsError = true
            });
        }
        #endregion

        #region Extended Logging Methods
        /// <summary>
        /// 记录事件发布
        /// </summary>
        public void LogEventPublished<T>(T eventData) where T : IModEvent
        {
            if (eventData == null)
            {
                LogError("Attempted to publish null event");
                return;
            }

            var message = $"Published: {eventData.GetType().Name} (ID: {eventData.EventId}) from {eventData.SenderId}";
            
            if (enableDebugLogging)
            {
                Debug.Log(prefix + message);
            }

            AddToHistory(new EventLogEntry
            {
                Timestamp = eventData.Timestamp,
                EventType = eventData.GetType().Name,
                EventId = eventData.EventId,
                SenderId = eventData.SenderId,
                Message = message,
                EventPhase = "Published"
            });
        }
        
        /// <summary>
        /// 记录事件处理
        /// </summary>
        public void LogEventHandled<T>(T eventData, object handler, bool success) where T : IModEvent
        {
            if (eventData == null)
            {
                LogError("Attempted to handle null event");
                return;
            }

            var handlerName = handler?.GetType().Name ?? "Unknown";
            var status = success ? "Success" : "Failed";
            var message = $"Handled: {eventData.GetType().Name} by {handlerName} - {status}";
            
            if (enableDebugLogging || !success)
            {
                if (success)
                    Debug.Log(prefix + message);
                else
                    Debug.LogWarning(prefix + message);
            }

            AddToHistory(new EventLogEntry
            {
                Timestamp = DateTime.Now,
                EventType = eventData.GetType().Name,
                EventId = eventData.EventId,
                SenderId = eventData.SenderId,
                Handler = handlerName,
                Message = message,
                EventPhase = "Handled",
                Success = success
            });
        }
        
        /// <summary>
        /// 记录事件订阅/取消订阅
        /// </summary>
        public void LogSubscriptionChanged<T>(object subscriber, bool added) where T : IModEvent
        {
            var subscriberName = subscriber?.GetType().Name ?? "Unknown";
            var action = added ? "subscribed to" : "unsubscribed from";
            var eventType = typeof(T).Name;
            var message = $"{subscriberName} {action} {eventType}";
            
            if (enableDebugLogging)
            {
                Debug.Log(prefix + message);
            }

            AddToHistory(new EventLogEntry
            {
                Timestamp = DateTime.Now,
                EventType = "Subscription",
                EventId = eventType,
                SenderId = subscriberName,
                Message = message,
                EventPhase = added ? "Subscribed" : "Unsubscribed"
            });
        }
        
        /// <summary>
        /// 记录带异常的错误
        /// </summary>
        public void LogException(Exception exception, string context = null)
        {
            var message = string.IsNullOrEmpty(context) 
                ? $"Exception: {exception.Message}" 
                : $"Exception in {context}: {exception.Message}";
                
            Debug.LogError(prefix + message);
            Debug.LogException(exception);

            AddToHistory(new EventLogEntry
            {
                Timestamp = DateTime.Now,
                EventType = "Exception",
                EventId = exception.GetType().Name,
                SenderId = "System",
                Message = message,
                IsError = true,
                StackTrace = exception.StackTrace
            });
        }
        #endregion

        #region History Management
        /// <summary>
        /// 添加到历史记录
        /// </summary>
        private void AddToHistory(EventLogEntry entry)
        {
            logHistory.Enqueue(entry);
            
            // 限制历史记录大小
            while (logHistory.Count > maxHistorySize)
            {
                logHistory.Dequeue();
            }

            // 如果启用了文件日志，写入文件
            if (logToFile)
            {
                WriteToFile(entry);
            }
        }

        /// <summary>
        /// 获取历史记录
        /// </summary>
        public EventLogEntry[] GetHistory()
        {
            return logHistory.ToArray();
        }

        /// <summary>
        /// 清空历史记录
        /// </summary>
        public void ClearHistory()
        {
            logHistory.Clear();
        }

        /// <summary>
        /// 获取错误历史
        /// </summary>
        public EventLogEntry[] GetErrorHistory()
        {
            return logHistory.Where(e => e.IsError).ToArray();
        }

        /// <summary>
        /// 获取特定事件类型的历史
        /// </summary>
        public EventLogEntry[] GetHistoryByEventType(string eventType)
        {
            return logHistory.Where(e => e.EventType == eventType).ToArray();
        }
        #endregion

        #region File Logging
        /// <summary>
        /// 写入日志文件
        /// </summary>
        private void WriteToFile(EventLogEntry entry)
        {
            try
            {
                var logPath = System.IO.Path.Combine(Application.persistentDataPath, "Logs");
                if (!System.IO.Directory.Exists(logPath))
                {
                    System.IO.Directory.CreateDirectory(logPath);
                }

                var fileName = $"EventLog_{DateTime.Now:yyyy-MM-dd}.log";
                var filePath = System.IO.Path.Combine(logPath, fileName);

                var logLine = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff} | " +
                             $"{entry.EventPhase ?? "Event"} | " +
                             $"{entry.EventType} | " +
                             $"{entry.EventId} | " +
                             $"{entry.SenderId} | " +
                             $"{entry.Message}";

                if (!string.IsNullOrEmpty(entry.StackTrace))
                {
                    logLine += $"\n{entry.StackTrace}";
                }

                System.IO.File.AppendAllText(filePath, logLine + "\n");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to write event log to file: {ex.Message}");
            }
        }
        #endregion

        #region Statistics
        /// <summary>
        /// 获取事件统计信息
        /// </summary>
        public EventStatistics GetStatistics()
        {
            var stats = new EventStatistics
            {
                TotalEvents = logHistory.Count,
                ErrorCount = logHistory.Count(e => e.IsError),
                SuccessCount = logHistory.Count(e => e.Success),
                FailureCount = logHistory.Count(e => !e.Success && !e.IsError)
            };

            // 按事件类型统计
            stats.EventTypeCounts = logHistory
                .GroupBy(e => e.EventType)
                .ToDictionary(g => g.Key, g => g.Count());

            // 按发送者统计
            stats.SenderCounts = logHistory
                .GroupBy(e => e.SenderId)
                .ToDictionary(g => g.Key, g => g.Count());

            return stats;
        }
        #endregion
    }

    #region Data Structures
    /// <summary>
    /// 事件日志条目
    /// </summary>
    [Serializable]
    public class EventLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; }
        public string EventId { get; set; }
        public string SenderId { get; set; }
        public string Handler { get; set; }
        public string Message { get; set; }
        public string EventPhase { get; set; }
        public bool Success { get; set; } = true;
        public bool IsError { get; set; }
        public string StackTrace { get; set; }
    }

    /// <summary>
    /// 事件统计信息
    /// </summary>
    [Serializable]
    public class EventStatistics
    {
        public int TotalEvents { get; set; }
        public int ErrorCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public Dictionary<string, int> EventTypeCounts { get; set; }
        public Dictionary<string, int> SenderCounts { get; set; }
    }
    #endregion
}