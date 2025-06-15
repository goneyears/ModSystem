using System;
using System.Collections.Generic;
using System.Text;

namespace ModSystem.Core.EventSystem
{
    /// <summary>
    /// 事件基类（可选使用）
    /// </summary>
    public abstract class ModEventBase : IModEvent
    {
        public abstract string EventId { get; }
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
