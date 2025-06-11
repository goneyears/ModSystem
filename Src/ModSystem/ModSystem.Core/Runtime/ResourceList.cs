using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 资源列表
    /// </summary>
    [Serializable]
    public class ResourceList
    {
        public string[] models { get; set; }
        public string[] objects { get; set; }
        public string[] configs { get; set; }
        public string[] textures { get; set; }
        public string[] audio { get; set; }
    }
} 