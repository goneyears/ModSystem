using System;
using System.Collections.Generic;
using System.Linq;

namespace ModSystem.Core
{
    /// <summary>
    /// 模组服务注册表实现
    /// 管理所有模组提供的服务
    /// </summary>
    public class ModServiceRegistry : IServiceRegistry
    {
        private readonly Dictionary<Type, Dictionary<string, IModService>> services;
        private readonly IEventBus eventBus;
        private readonly ILogger logger;
        private readonly object lockObject = new object();
        
        /// <summary>
        /// 创建服务注册表
        /// </summary>
        /// <param name="eventBus">事件总线</param>
        /// <param name="logger">日志记录器</param>
        public ModServiceRegistry(IEventBus eventBus, ILogger logger = null)
        {
            this.services = new Dictionary<Type, Dictionary<string, IModService>>();
            this.eventBus = eventBus;
            this.logger = logger;
        }
        
        /// <summary>
        /// 注册服务
        /// </summary>
        public void RegisterService<T>(T service) where T : class, IModService
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));
            
            var serviceType = typeof(T);
            
            lock (lockObject)
            {
                if (!services.ContainsKey(serviceType))
                {
                    services[serviceType] = new Dictionary<string, IModService>();
                }
                
                if (services[serviceType].ContainsKey(service.ServiceId))
                {
                    logger?.LogWarning($"Service {service.ServiceId} is already registered, replacing...");
                }
                
                services[serviceType][service.ServiceId] = service;
            }
            
            // 发布服务注册事件
            eventBus?.Publish(new ServiceRegisteredEvent
            {
                ServiceType = serviceType.Name,
                ServiceId = service.ServiceId,
                ProviderId = service.ProviderId,
                Version = service.Version
            });
            
            logger?.Log($"Service registered: {serviceType.Name} - {service.ServiceId}");
        }
        
        /// <summary>
        /// 获取服务（返回第一个）
        /// </summary>
        public T GetService<T>() where T : class, IModService
        {
            var serviceType = typeof(T);
            
            lock (lockObject)
            {
                if (services.ContainsKey(serviceType) && services[serviceType].Count > 0)
                {
                    return services[serviceType].Values.First() as T;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 获取指定ID的服务
        /// </summary>
        public T GetService<T>(string serviceId) where T : class, IModService
        {
            var serviceType = typeof(T);
            
            lock (lockObject)
            {
                if (services.ContainsKey(serviceType) && 
                    services[serviceType].TryGetValue(serviceId, out var service))
                {
                    return service as T;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 获取所有服务
        /// </summary>
        public IEnumerable<T> GetServices<T>() where T : class, IModService
        {
            var serviceType = typeof(T);
            
            lock (lockObject)
            {
                if (services.ContainsKey(serviceType))
                {
                    return services[serviceType].Values.Cast<T>().ToList();
                }
            }
            
            return Enumerable.Empty<T>();
        }
        
        /// <summary>
        /// 注销服务
        /// </summary>
        public bool UnregisterService<T>(string serviceId) where T : class, IModService
        {
            var serviceType = typeof(T);
            IModService removedService = null;
            
            lock (lockObject)
            {
                if (services.ContainsKey(serviceType) && 
                    services[serviceType].TryGetValue(serviceId, out removedService))
                {
                    services[serviceType].Remove(serviceId);
                    
                    if (services[serviceType].Count == 0)
                    {
                        services.Remove(serviceType);
                    }
                }
            }
            
            if (removedService != null)
            {
                // 发布服务注销事件
                eventBus?.Publish(new ServiceUnregisteredEvent
                {
                    ServiceType = serviceType.Name,
                    ServiceId = serviceId,
                    ProviderId = removedService.ProviderId
                });
                
                logger?.Log($"Service unregistered: {serviceType.Name} - {serviceId}");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 检查服务是否已注册
        /// </summary>
        public bool IsServiceRegistered<T>() where T : class, IModService
        {
            var serviceType = typeof(T);
            
            lock (lockObject)
            {
                return services.ContainsKey(serviceType) && services[serviceType].Count > 0;
            }
        }
        
        /// <summary>
        /// 检查指定ID的服务是否已注册
        /// </summary>
        public bool IsServiceRegistered<T>(string serviceId) where T : class, IModService
        {
            var serviceType = typeof(T);
            
            lock (lockObject)
            {
                return services.ContainsKey(serviceType) && 
                       services[serviceType].ContainsKey(serviceId);
            }
        }
        
        /// <summary>
        /// 获取服务统计信息
        /// </summary>
        public Dictionary<string, int> GetServiceStatistics()
        {
            lock (lockObject)
            {
                return services.ToDictionary(
                    kvp => kvp.Key.Name,
                    kvp => kvp.Value.Count
                );
            }
        }
    }
} 