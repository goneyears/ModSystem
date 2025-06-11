using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 模组依赖项
    /// </summary>
    [Serializable]
    public class ModDependency
    {
        public string id { get; set; }
        public string version { get; set; }
        public bool optional { get; set; }
    }
} 