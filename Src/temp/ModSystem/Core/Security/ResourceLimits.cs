namespace ModSystem.Core
{
    /// <summary>
    /// 资源限制配置
    /// </summary>
    public class ResourceLimits
    {
        /// <summary>
        /// 最大内存使用（MB）
        /// </summary>
        public int MaxMemoryMB { get; set; } = 100;
        
        /// <summary>
        /// 最大CPU时间（毫秒）
        /// </summary>
        public int MaxCpuTimeMs { get; set; } = 10;
        
        /// <summary>
        /// 最大对象数量
        /// </summary>
        public int MaxObjects { get; set; } = 50;
        
        /// <summary>
        /// 最大文件大小（字节）
        /// </summary>
        public int MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10MB
        
        /// <summary>
        /// 最大事件发送速率（每秒）
        /// </summary>
        public int MaxEventRate { get; set; } = 100;
        
        /// <summary>
        /// 最大服务调用数（每分钟）
        /// </summary>
        public int MaxServiceCalls { get; set; } = 1000;
    }
} 