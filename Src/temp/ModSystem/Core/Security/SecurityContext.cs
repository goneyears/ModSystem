using System.Collections.Generic;
using System.Linq;

namespace ModSystem.Core
{
    /// <summary>
    /// 安全上下文实现
    /// </summary>
    public class SecurityContext : ISecurityContext
    {
        /// <summary>
        /// 模组ID
        /// </summary>
        public string ModId { get; set; }
        
        /// <summary>
        /// 授予的权限集合
        /// </summary>
        public HashSet<string> Permissions { get; set; }
        
        /// <summary>
        /// 资源限制
        /// </summary>
        public ResourceLimits ResourceLimits { get; set; }
        
        /// <summary>
        /// 检查是否具有权限
        /// </summary>
        public bool HasPermission(string permission)
        {
            return Permissions?.Contains(permission) ?? false;
        }
        
        /// <summary>
        /// 获取所有权限
        /// </summary>
        public IReadOnlyCollection<string> GetPermissions()
        {
            return Permissions?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
        }
        
        /// <summary>
        /// 获取资源限制
        /// </summary>
        ResourceLimits ISecurityContext.GetResourceLimits()
        {
            return ResourceLimits ?? new ResourceLimits();
        }
    }
} 