using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 模组请求基类
    /// </summary>
    public abstract class ModRequest : IModEvent
    {
        public string EventId => GetType().Name;
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// 请求的唯一标识符
        /// </summary>
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
    }
} 