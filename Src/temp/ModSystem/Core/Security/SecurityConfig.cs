using System.Collections.Generic;

namespace ModSystem.Core
{
    /// <summary>
    /// 安全配置
    /// </summary>
    public class SecurityConfig
    {
        /// <summary>
        /// 是否要求模组签名
        /// </summary>
        public bool RequireSignedMods { get; set; } = true;
        
        /// <summary>
        /// 公钥文件路径
        /// </summary>
        public string PublicKeyPath { get; set; }
        
        /// <summary>
        /// 模组目录
        /// </summary>
        public string ModDirectory { get; set; } = "Mods";
        
        /// <summary>
        /// 允许的模组路径列表
        /// </summary>
        public List<string> AllowedModPaths { get; set; }
        
        /// <summary>
        /// 允许的权限集合
        /// </summary>
        public HashSet<string> AllowedPermissions { get; set; } = new HashSet<string>
        {
            "event_publish",
            "event_subscribe", 
            "service_register",
            "object_create",
            "config_read",
            "audio_play",
            "ui_create"
        };
        
        /// <summary>
        /// 默认权限集合
        /// </summary>
        public HashSet<string> DefaultPermissions { get; set; } = new HashSet<string>
        {
            "event_publish",
            "event_subscribe",
            "config_read"
        };
        
        /// <summary>
        /// 模组权限配置
        /// </summary>
        public Dictionary<string, List<string>> ModPermissions { get; set; }
        
        /// <summary>
        /// 模组资源限制
        /// </summary>
        public Dictionary<string, ResourceLimits> ModResourceLimits { get; set; }
        
        /// <summary>
        /// 信任的模组列表
        /// </summary>
        public List<string> TrustedMods { get; set; }
    }
} 