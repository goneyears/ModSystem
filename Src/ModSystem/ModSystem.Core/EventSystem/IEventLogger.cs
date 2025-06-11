namespace ModSystem.Core
{
    /// <summary>
    /// 事件日志记录接口
    /// </summary>
    public interface IEventLogger
    {
        void LogEvent(IModEvent e);
        void LogSubscription(string eventType, string subscriber);
        void LogError(string message);
    }
} 