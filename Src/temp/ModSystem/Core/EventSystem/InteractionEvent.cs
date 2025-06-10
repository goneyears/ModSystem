using System;
using System.Collections.Generic;

namespace ModSystem.Core
{
    /// <summary>
    /// 交互事件
    /// </summary>
    public class InteractionEvent : IModEvent
    {
        public string EventId => "interaction";
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
        
        public string TargetId { get; set; }
        public InteractionType InteractionType { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }
} 