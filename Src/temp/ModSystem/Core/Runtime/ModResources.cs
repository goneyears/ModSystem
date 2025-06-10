using System.Collections.Generic;

namespace ModSystem.Core
{
    /// <summary>
    /// 模组资源容器
    /// </summary>
    public class ModResources
    {
        /// <summary>
        /// 对象定义字典
        /// </summary>
        public Dictionary<string, ObjectDefinition> ObjectDefinitions { get; set; } 
            = new Dictionary<string, ObjectDefinition>();
        
        /// <summary>
        /// 配置文件字典
        /// </summary>
        public Dictionary<string, string> Configs { get; set; } 
            = new Dictionary<string, string>();
        
        /// <summary>
        /// 模型路径字典
        /// </summary>
        public Dictionary<string, string> ModelPaths { get; set; } 
            = new Dictionary<string, string>();
        
        /// <summary>
        /// 纹理路径字典
        /// </summary>
        public Dictionary<string, string> TexturePaths { get; set; } 
            = new Dictionary<string, string>();
        
        /// <summary>
        /// 音频路径字典
        /// </summary>
        public Dictionary<string, string> AudioPaths { get; set; } 
            = new Dictionary<string, string>();
        
        /// <summary>
        /// 获取配置对象
        /// </summary>
        public T GetConfig<T>(string configName)
        {
            if (Configs.TryGetValue(configName, out var json))
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            }
            return default(T);
        }
    }
} 