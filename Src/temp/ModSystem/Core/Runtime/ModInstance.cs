using System;
using System.Collections.Generic;

namespace ModSystem.Core
{
    /// <summary>
    /// 模组实例
    /// 表示一个已加载的模组
    /// </summary>
    public class ModInstance
    {
        /// <summary>
        /// 加载的模组数据
        /// </summary>
        public LoadedMod LoadedMod { get; set; }
        
        /// <summary>
        /// 模组状态
        /// </summary>
        public ModState State { get; set; }
        
        /// <summary>
        /// 加载时间
        /// </summary>
        public DateTime LoadTime { get; set; }
        
        /// <summary>
        /// 游戏对象列表（Unity层使用）
        /// </summary>
        public List<object> GameObjects { get; set; } = new List<object>();
    }
} 