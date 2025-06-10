using System.Collections.Generic;
using System.Reflection;

namespace ModSystem.Core
{
    /// <summary>
    /// 已加载的模组数据
    /// </summary>
    public class LoadedMod
    {
        /// <summary>
        /// 模组清单
        /// </summary>
        public ModManifest Manifest { get; set; }
        
        /// <summary>
        /// 模组程序集
        /// </summary>
        public Assembly Assembly { get; set; }
        
        /// <summary>
        /// 模组资源
        /// </summary>
        public ModResources Resources { get; set; }
        
        /// <summary>
        /// 模组行为列表
        /// </summary>
        public List<IModBehaviour> Behaviours { get; set; } = new List<IModBehaviour>();
        
        /// <summary>
        /// 模组根目录
        /// </summary>
        public string RootPath { get; set; }
        
        /// <summary>
        /// 是否为临时模组
        /// </summary>
        public bool IsTemporary { get; set; }
    }
} 