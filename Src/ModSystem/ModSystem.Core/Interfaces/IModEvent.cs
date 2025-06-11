using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 模组事件基础接口
    /// 所有模组事件都必须实现此接口
    /// </summary>
    public interface IModEvent
    {
        /// <summary>
        /// 事件的唯一标识符
        /// </summary>
        string EventId { get; }
        
        /// <summary>
        /// 发送事件的模组或组件ID
        /// </summary>
        string SenderId { get; set; }
        
        /// <summary>
        /// 事件发生的时间戳
        /// </summary>
        DateTime Timestamp { get; set; }
    }
} 