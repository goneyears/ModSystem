// Src/ModSystem/ModSystem.Core/EventSystem/IEventLogger.cs
namespace ModSystem.Core
{
    /// <summary>
    /// 事件日志记录接口
    /// </summary>
    public interface IEventLogger
    {
        /// <summary>
        /// 记录事件
        /// </summary>
        /// <param name="e">事件对象</param>
        void LogEvent(IModEvent e);

        /// <summary>
        /// 记录事件订阅
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <param name="subscriber">订阅者标识</param>
        void LogSubscription(string eventType, string subscriber);

        /// <summary>
        /// 记录错误
        /// </summary>
        /// <param name="message">错误消息</param>
        void LogError(string message);
    }
}