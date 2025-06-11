using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 紧急停止事件
    /// </summary>
    public class EmergencyStopEvent : IModEvent
    {
        public string EventId => "emergency_stop";
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
        
        public string Reason { get; set; }
    }
} 