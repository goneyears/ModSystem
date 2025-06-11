using System;
using System.Collections.Generic;

namespace ModSystem.Core
{
    /// <summary>
    /// 对象定义，替代Unity的Prefab系统
    /// </summary>
    [Serializable]
    public class ObjectDefinition
    {
        /// <summary>
        /// 对象唯一标识符
        /// </summary>
        public string objectId { get; set; }
        
        /// <summary>
        /// 对象名称
        /// </summary>
        public string name { get; set; }
        
        /// <summary>
        /// 组件定义列表
        /// </summary>
        public List<ComponentDefinition> components { get; set; }
    }
} 