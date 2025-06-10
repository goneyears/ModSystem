using System.Collections.Generic;

namespace ModSystem.Core
{
    /// <summary>
    /// 安全上下文接口
    /// 提供模组的权限检查功能
    /// </summary>
    public interface ISecurityContext
    {
        /// <summary>
        /// 模组ID
        /// </summary>
        string ModId { get; }
        
        /// <summary>
        /// 检查是否具有指定权限
        /// </summary>
        /// <param name="permission">权限名称</param>
        /// <returns>是否具有权限</returns>
        bool HasPermission(string permission);
        
        /// <summary>
        /// 获取所有已授予的权限
        /// </summary>
        /// <returns>权限集合</returns>
        IReadOnlyCollection<string> GetPermissions();
        
        /// <summary>
        /// 获取资源限制
        /// </summary>
        /// <returns>资源限制配置</returns>
        ResourceLimits GetResourceLimits();
    }
} 