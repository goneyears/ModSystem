using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 模组元数据
    /// </summary>
    [Serializable]
    public class ModMetadata
    {
        public string[] tags { get; set; }
        public string category { get; set; }
        public string homepage { get; set; }
    }
} 