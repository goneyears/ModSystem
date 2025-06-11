using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 服务注销事件
    /// </summary>
    public class ServiceUnregisteredEvent : IModEvent
    {
        public string EventId => "service_unregistered";
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
        
        public string ServiceType { get; set; }
        public string ServiceId { get; set; }
        public string ProviderId { get; set; }
    }
} 