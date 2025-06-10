using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 模组错误事件
    /// </summary>
    public class ModErrorEvent : IModEvent
    {
        public string EventId => "mod_error";
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
        
        public string ErrorType { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }
} 