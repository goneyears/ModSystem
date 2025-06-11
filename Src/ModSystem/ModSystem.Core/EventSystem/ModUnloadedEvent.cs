using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 模组卸载事件
    /// </summary>
    public class ModUnloadedEvent : IModEvent
    {
        public string EventId => "mod_unloaded";
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
        
        public string ModId { get; set; }
        public string Reason { get; set; }
    }
} 