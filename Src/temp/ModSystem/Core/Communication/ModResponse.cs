using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 模组响应基类
    /// </summary>
    public abstract class ModResponse : IModEvent
    {
        public string EventId => GetType().Name;
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// 对应的请求ID
        /// </summary>
        public string RequestId { get; set; }
        
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// 响应消息
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// 响应结果
        /// </summary>
        public object Result { get; set; }
    }
} 