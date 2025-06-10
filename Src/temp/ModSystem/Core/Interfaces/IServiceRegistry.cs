using System.Collections.Generic;

namespace ModSystem.Core
{
    /// <summary>
    /// 服务注册表接口
    /// 管理所有已注册的服务
    /// </summary>
    public interface IServiceRegistry
    {
        /// <summary>
        /// 注册服务
        /// </summary>
        /// <typeparam name="T">服务接口类型</typeparam>
        /// <param name="service">服务实例</param>
        void RegisterService<T>(T service) where T : class, IModService;
        
        /// <summary>
        /// 获取服务（返回第一个匹配的服务）
        /// </summary>
        /// <typeparam name="T">服务接口类型</typeparam>
        /// <returns>服务实例，如果不存在则返回null</returns>
        T GetService<T>() where T : class, IModService;
        
        /// <summary>
        /// 获取指定ID的服务
        /// </summary>
        /// <typeparam name="T">服务接口类型</typeparam>
        /// <param name="serviceId">服务ID</param>
        /// <returns>服务实例，如果不存在则返回null</returns>
        T GetService<T>(string serviceId) where T : class, IModService;
        
        /// <summary>
        /// 获取所有指定类型的服务
        /// </summary>
        /// <typeparam name="T">服务接口类型</typeparam>
        /// <returns>服务实例集合</returns>
        System.Collections.Generic.IEnumerable<T> GetServices<T>() where T : class, IModService;
        
        /// <summary>
        /// 注销服务
        /// </summary>
        /// <typeparam name="T">服务接口类型</typeparam>
        /// <param name="serviceId">服务ID</param>
        /// <returns>是否成功注销</returns>
        bool UnregisterService<T>(string serviceId) where T : class, IModService;
        
        /// <summary>
        /// 检查服务是否已注册
        /// </summary>
        /// <typeparam name="T">服务接口类型</typeparam>
        /// <returns>是否已注册</returns>
        bool IsServiceRegistered<T>() where T : class, IModService;
        
        /// <summary>
        /// 检查指定ID的服务是否已注册
        /// </summary>
        /// <typeparam name="T">服务接口类型</typeparam>
        /// <param name="serviceId">服务ID</param>
        /// <returns>是否已注册</returns>
        bool IsServiceRegistered<T>(string serviceId) where T : class, IModService;
    }
} 