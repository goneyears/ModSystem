using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ModSystem.Core
{
    /// <summary>
    /// 组件定义
    /// </summary>
    [Serializable]
    public class ComponentDefinition
    {
        /// <summary>
        /// 组件类型
        /// </summary>
        public string type { get; set; }
        
        /// <summary>
        /// 组件属性
        /// </summary>
        public Dictionary<string, object> properties { get; set; }
        
        /// <summary>
        /// 获取属性值
        /// </summary>
        public T GetProperty<T>(string key, T defaultValue = default)
        {
            if (properties != null && properties.TryGetValue(key, out var value))
            {
                try
                {
                    // 处理JSON数组
                    if (value is JArray jArray && typeof(T).IsArray)
                    {
                        var elementType = typeof(T).GetElementType();
                        var array = jArray.ToObject(typeof(T));
                        return (T)array;
                    }
                    
                    // 处理其他类型
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }
} 