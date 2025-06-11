using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 服务注册事件
    /// </summary>
    public class ServiceRegisteredEvent : IModEvent
    {
        public string EventId => "service_registered";
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
        
        public string ServiceType { get; set; }
        public string ServiceId { get; set; }
        public string ProviderId { get; set; }
        public string Version { get; set; }
    }
} 