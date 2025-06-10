using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 模组清单
    /// 定义模组的元数据
    /// </summary>
    [Serializable]
    public class ModManifest
    {
        /// <summary>
        /// 模组唯一标识符
        /// </summary>
        public string id { get; set; }
        
        /// <summary>
        /// 模组名称
        /// </summary>
        public string name { get; set; }
        
        /// <summary>
        /// 版本号
        /// </summary>
        public string version { get; set; }
        
        /// <summary>
        /// 作者
        /// </summary>
        public string author { get; set; }
        
        /// <summary>
        /// 描述
        /// </summary>
        public string description { get; set; }
        
        /// <summary>
        /// Unity版本要求
        /// </summary>
        public string unity_version { get; set; }
        
        /// <summary>
        /// SDK版本要求
        /// </summary>
        public string sdk_version { get; set; }
        
        /// <summary>
        /// 主行为类名
        /// </summary>
        public string main_class { get; set; }
        
        /// <summary>
        /// 额外的行为类
        /// </summary>
        public string[] behaviours { get; set; }
        
        /// <summary>
        /// 依赖项
        /// </summary>
        public ModDependency[] dependencies { get; set; }
        
        /// <summary>
        /// 提供的服务
        /// </summary>
        public ServiceDefinition[] services { get; set; }
        
        /// <summary>
        /// 请求的权限
        /// </summary>
        public string[] permissions { get; set; }
        
        /// <summary>
        /// 资源列表
        /// </summary>
        public ResourceList resources { get; set; }
        
        /// <summary>
        /// 元数据
        /// </summary>
        public ModMetadata metadata { get; set; }
    }
} 