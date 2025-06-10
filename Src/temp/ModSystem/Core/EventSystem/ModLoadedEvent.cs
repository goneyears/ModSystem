using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 模组加载事件
    /// </summary>
    public class ModLoadedEvent : IModEvent
    {
        public string EventId => "mod_loaded";
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
        
        public string ModId { get; set; }
        public string ModName { get; set; }
        public string Version { get; set; }
    }
} 