using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 服务定义
    /// </summary>
    [Serializable]
    public class ServiceDefinition
    {
        public string @interface { get; set; }
        public string implementation { get; set; }
    }
} 