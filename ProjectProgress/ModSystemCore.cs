// ================================================================================
// Interfaces/IPlatformAbstractions.cs
// ================================================================================

using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 日志接口，用于替代Unity的Debug类
    /// 允许在不同平台实现自定义日志逻辑
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// 记录普通日志信息
        /// </summary>
        /// <param name="message">日志消息</param>
        void Log(string message);
        
        /// <summary>
        /// 记录警告信息
        /// </summary>
        /// <param name="message">警告消息</param>
        void LogWarning(string message);
        
        /// <summary>
        /// 记录错误信息
        /// </summary>
        /// <param name="message">错误消息</param>
        void LogError(string message);
    }
    
    /// <summary>
    /// 路径提供接口，用于替代Unity的Application类
    /// 提供平台无关的路径访问
    /// </summary>
    public interface IPathProvider
    {
        /// <summary>
        /// 获取模组存放路径
        /// </summary>
        /// <returns>模组目录路径</returns>
        string GetModsPath();
        
        /// <summary>
        /// 获取配置文件路径
        /// </summary>
        /// <returns>配置目录路径</returns>
        string GetConfigPath();
        
        /// <summary>
        /// 获取临时文件路径
        /// </summary>
        /// <returns>临时目录路径</returns>
        string GetTempPath();
        
        /// <summary>
        /// 获取持久化数据路径
        /// </summary>
        /// <returns>持久化数据目录路径</returns>
        string GetPersistentDataPath();
    }
    
    /// <summary>
    /// 游戏对象接口，用于抽象Unity的GameObject
    /// 提供平台无关的对象操作
    /// </summary>
    public interface IGameObject
    {
        /// <summary>
        /// 对象名称
        /// </summary>
        string Name { get; set; }
        
        /// <summary>
        /// 对象是否激活
        /// </summary>
        bool IsActive { get; set; }
        
        /// <summary>
        /// 对象的变换组件
        /// </summary>
        ITransform Transform { get; }
        
        /// <summary>
        /// 获取指定类型的组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件实例，如果不存在则返回null</returns>
        T GetComponent<T>() where T : class;
        
        /// <summary>
        /// 添加指定类型的组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>新添加的组件实例</returns>
        T AddComponent<T>() where T : class;
    }
    
    /// <summary>
    /// 变换接口，用于抽象Unity的Transform
    /// 提供位置、旋转和缩放控制
    /// </summary>
    public interface ITransform
    {
        /// <summary>
        /// 世界空间位置
        /// </summary>
        Vector3 Position { get; set; }
        
        /// <summary>
        /// 世界空间旋转
        /// </summary>
        Quaternion Rotation { get; set; }
        
        /// <summary>
        /// 局部缩放
        /// </summary>
        Vector3 Scale { get; set; }
        
        /// <summary>
        /// 父变换
        /// </summary>
        ITransform Parent { get; set; }
    }
    
    /// <summary>
    /// 三维向量结构，兼容Unity的Vector3
    /// </summary>
    [Serializable]
    public struct Vector3
    {
        public float x;
        public float y;
        public float z;
        
        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        
        public static Vector3 Zero => new Vector3(0, 0, 0);
        public static Vector3 One => new Vector3(1, 1, 1);
        
        public static float Distance(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dy = a.y - b.y;
            var dz = a.z - b.z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
    
    /// <summary>
    /// 四元数结构，兼容Unity的Quaternion
    /// </summary>
    [Serializable]
    public struct Quaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;
        
        public Quaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
        
        public static Quaternion Identity => new Quaternion(0, 0, 0, 1);
        
        public static Quaternion Euler(float x, float y, float z)
        {
            // 简化的欧拉角转四元数实现
            return Identity; // 实际实现需要正确的数学计算
        }
    }
}

// ================================================================================
// Interfaces/IModEvent.cs
// ================================================================================

using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 模组事件基础接口
    /// 所有模组事件都必须实现此接口
    /// </summary>
    public interface IModEvent
    {
        /// <summary>
        /// 事件的唯一标识符
        /// </summary>
        string EventId { get; }
        
        /// <summary>
        /// 发送事件的模组或组件ID
        /// </summary>
        string SenderId { get; set; }
        
        /// <summary>
        /// 事件发生的时间戳
        /// </summary>
        DateTime Timestamp { get; set; }
    }
    
    /// <summary>
    /// 事件总线接口
    /// 提供事件的发布和订阅功能
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 订阅指定类型的事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        void Subscribe<T>(Action<T> handler) where T : IModEvent;
        
        /// <summary>
        /// 订阅指定类型的事件（带过滤条件）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        /// <param name="filter">过滤条件</param>
        void Subscribe<T>(Action<T> handler, Predicate<T> filter) where T : IModEvent;
        
        /// <summary>
        /// 发布事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        void Publish<T>(T eventData) where T : IModEvent;
        
        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">要取消的事件处理器</param>
        void Unsubscribe<T>(Action<T> handler) where T : IModEvent;
        
        /// <summary>
        /// 取消指定订阅者的所有事件订阅
        /// </summary>
        /// <param name="subscriber">订阅者对象</param>
        void UnsubscribeAll(object subscriber);
    }
}

// ================================================================================
// Interfaces/IModBehaviour.cs
// ================================================================================

namespace ModSystem.Core
{
    /// <summary>
    /// 模组行为接口
    /// 定义模组的主要逻辑和生命周期方法
    /// </summary>
    public interface IModBehaviour
    {
        /// <summary>
        /// 行为的唯一标识符
        /// </summary>
        string BehaviourId { get; }
        
        /// <summary>
        /// 行为版本号
        /// </summary>
        string Version { get; }
        
        /// <summary>
        /// 初始化方法，在模组加载时调用
        /// </summary>
        /// <param name="context">模组上下文</param>
        void OnInitialize(IModContext context);
        
        /// <summary>
        /// 更新方法，每帧调用
        /// </summary>
        /// <param name="deltaTime">自上次更新以来的时间（秒）</param>
        void OnUpdate(float deltaTime);
        
        /// <summary>
        /// 销毁方法，在模组卸载时调用
        /// </summary>
        void OnDestroy();
    }
}

// ================================================================================
// Interfaces/IObjectBehaviour.cs
// ================================================================================

using System.Collections.Generic;

namespace ModSystem.Core
{
    /// <summary>
    /// 对象附加行为接口
    /// 用于为通过ObjectFactory创建的对象添加自定义行为
    /// </summary>
    public interface IObjectBehaviour
    {
        /// <summary>
        /// 当行为附加到游戏对象时调用
        /// </summary>
        /// <param name="gameObject">目标游戏对象</param>
        void OnAttach(IGameObject gameObject);
        
        /// <summary>
        /// 配置行为参数
        /// </summary>
        /// <param name="config">配置参数字典</param>
        void OnConfigure(Dictionary<string, object> config);
        
        /// <summary>
        /// 当行为从游戏对象分离时调用
        /// </summary>
        void OnDetach();
    }
}

// ================================================================================
// Interfaces/IModContext.cs
// ================================================================================

namespace ModSystem.Core
{
    /// <summary>
    /// 模组上下文接口
    /// 为模组提供运行时环境和API访问
    /// </summary>
    public interface IModContext
    {
        /// <summary>
        /// 当前模组的ID
        /// </summary>
        string ModId { get; }
        
        /// <summary>
        /// 模组的主游戏对象
        /// </summary>
        IGameObject GameObject { get; }
        
        /// <summary>
        /// 事件总线引用
        /// </summary>
        IEventBus EventBus { get; }
        
        /// <summary>
        /// 模组API访问
        /// </summary>
        IModAPI API { get; }
        
        /// <summary>
        /// 服务注册表引用
        /// </summary>
        IServiceRegistry Services { get; }
        
        /// <summary>
        /// 安全上下文
        /// </summary>
        ISecurityContext SecurityContext { get; }
        
        /// <summary>
        /// 获取指定类型的组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件实例</returns>
        T GetComponent<T>() where T : class;
        
        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="message">日志消息</param>
        void Log(string message);
        
        /// <summary>
        /// 记录错误
        /// </summary>
        /// <param name="message">错误消息</param>
        void LogError(string message);
    }
    
    /// <summary>
    /// 模组API接口
    /// 提供模组可用的各种功能
    /// </summary>
    public interface IModAPI
    {
        /// <summary>
        /// 请求响应管理器
        /// </summary>
        IRequestResponseManager RequestResponse { get; }
        
        /// <summary>
        /// 对象工厂
        /// </summary>
        IObjectFactory ObjectFactory { get; }
        
        /// <summary>
        /// 工具类
        /// </summary>
        IModUtilities Utilities { get; }
    }
    
    /// <summary>
    /// 模组工具接口
    /// 提供辅助功能
    /// </summary>
    public interface IModUtilities
    {
        /// <summary>
        /// 启动协程
        /// </summary>
        /// <param name="enumerator">协程枚举器</param>
        /// <returns>协程句柄</returns>
        object StartCoroutine(System.Collections.IEnumerator enumerator);
        
        /// <summary>
        /// 停止协程
        /// </summary>
        /// <param name="coroutine">协程句柄</param>
        void StopCoroutine(object coroutine);
    }
}

// ================================================================================
// Interfaces/IModService.cs
// ================================================================================

namespace ModSystem.Core
{
    /// <summary>
    /// 模组服务接口
    /// 所有可注册的服务都必须实现此接口
    /// </summary>
    public interface IModService
    {
        /// <summary>
        /// 服务的唯一标识符
        /// </summary>
        string ServiceId { get; }
        
        /// <summary>
        /// 提供服务的模组ID
        /// </summary>
        string ProviderId { get; }
        
        /// <summary>
        /// 服务版本号
        /// </summary>
        string Version { get; }
    }
    
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

// ================================================================================
// Interfaces/IObjectFactory.cs
// ================================================================================

using System.Threading.Tasks;

namespace ModSystem.Core
{
    /// <summary>
    /// 平台无关的对象工厂接口
    /// 用于创建游戏对象
    /// </summary>
    public interface IObjectFactory
    {
        /// <summary>
        /// 从定义文件创建对象
        /// </summary>
        /// <param name="definitionPath">对象定义文件路径</param>
        /// <returns>创建的游戏对象</returns>
        Task<IGameObject> CreateObjectAsync(string definitionPath);
        
        /// <summary>
        /// 从对象定义创建对象
        /// </summary>
        /// <param name="definition">对象定义</param>
        /// <returns>创建的游戏对象</returns>
        Task<IGameObject> CreateObjectFromDefinitionAsync(ObjectDefinition definition);
    }
}

// ================================================================================
// Interfaces/ISecurityContext.cs
// ================================================================================

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

// ================================================================================
// EventSystem/ModEventBus.cs
// ================================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace ModSystem.Core
{
    /// <summary>
    /// 事件日志记录接口
    /// </summary>
    public interface IEventLogger
    {
        void LogEvent(IModEvent e);
        void LogSubscription(string eventType, string subscriber);
        void LogError(string message);
    }
    
    /// <summary>
    /// 模组事件总线实现
    /// 提供线程安全的事件发布和订阅功能
    /// </summary>
    public class ModEventBus : IEventBus
    {
        private readonly Dictionary<Type, List<EventHandler>> handlers;
        private readonly object lockObject = new object();
        private readonly IEventLogger logger;
        
        /// <summary>
        /// 事件发布时触发的事件
        /// </summary>
        public event Action<IModEvent> OnEventPublished;
        
        /// <summary>
        /// 内部事件处理器包装类
        /// </summary>
        private class EventHandler
        {
            public Delegate Handler { get; set; }
            public Predicate<IModEvent> Filter { get; set; }
            public string SubscriberId { get; set; }
            public WeakReference TargetRef { get; set; }
        }
        
        /// <summary>
        /// 创建事件总线实例
        /// </summary>
        /// <param name="logger">可选的事件日志记录器</param>
        public ModEventBus(IEventLogger logger = null)
        {
            handlers = new Dictionary<Type, List<EventHandler>>();
            this.logger = logger;
        }
        
        /// <summary>
        /// 订阅事件
        /// </summary>
        public void Subscribe<T>(Action<T> handler) where T : IModEvent
        {
            Subscribe(handler, null);
        }
        
        /// <summary>
        /// 订阅事件（带过滤器）
        /// </summary>
        public void Subscribe<T>(Action<T> handler, Predicate<T> filter) where T : IModEvent
        {
            lock (lockObject)
            {
                var eventType = typeof(T);
                if (!handlers.ContainsKey(eventType))
                {
                    handlers[eventType] = new List<EventHandler>();
                }
                
                handlers[eventType].Add(new EventHandler
                {
                    Handler = handler,
                    Filter = filter != null ? e => filter((T)e) : null,
                    SubscriberId = handler.Target?.GetType().Name ?? "Anonymous",
                    TargetRef = handler.Target != null ? new WeakReference(handler.Target) : null
                });
                
                logger?.LogSubscription(eventType.Name, handler.Target?.GetType().Name);
            }
        }
        
        /// <summary>
        /// 发布事件
        /// </summary>
        public void Publish<T>(T eventData) where T : IModEvent
        {
            if (eventData == null) return;
            
            eventData.Timestamp = DateTime.Now;
            logger?.LogEvent(eventData);
            OnEventPublished?.Invoke(eventData);
            
            List<EventHandler> eventHandlers;
            lock (lockObject)
            {
                var eventType = typeof(T);
                if (!handlers.ContainsKey(eventType))
                    return;
                
                // 清理已释放的处理器
                handlers[eventType].RemoveAll(h => 
                    h.TargetRef != null && !h.TargetRef.IsAlive);
                
                eventHandlers = handlers[eventType].ToList();
            }
            
            // 在锁外执行处理器，避免死锁
            foreach (var handler in eventHandlers)
            {
                try
                {
                    if (handler.Filter != null && !handler.Filter(eventData))
                        continue;
                    
                    ((Action<T>)handler.Handler)?.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    logger?.LogError($"Handler error for {typeof(T).Name}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 取消订阅
        /// </summary>
        public void Unsubscribe<T>(Action<T> handler) where T : IModEvent
        {
            lock (lockObject)
            {
                var eventType = typeof(T);
                if (!handlers.ContainsKey(eventType))
                    return;
                
                handlers[eventType].RemoveAll(h => h.Handler.Equals(handler));
                
                if (handlers[eventType].Count == 0)
                    handlers.Remove(eventType);
            }
        }
        
        /// <summary>
        /// 取消所有订阅
        /// </summary>
        public void UnsubscribeAll(object subscriber)
        {
            lock (lockObject)
            {
                foreach (var handlerList in handlers.Values)
                {
                    handlerList.RemoveAll(h => 
                        h.TargetRef != null && 
                        h.TargetRef.IsAlive && 
                        h.TargetRef.Target == subscriber);
                }
                
                // 清理空列表
                var emptyKeys = handlers.Where(kvp => kvp.Value.Count == 0)
                    .Select(kvp => kvp.Key).ToList();
                foreach (var key in emptyKeys)
                {
                    handlers.Remove(key);
                }
            }
        }
        
        /// <summary>
        /// 获取事件统计信息
        /// </summary>
        public Dictionary<string, int> GetEventStatistics()
        {
            lock (lockObject)
            {
                return handlers.ToDictionary(
                    kvp => kvp.Key.Name,
                    kvp => kvp.Value.Count
                );
            }
        }
    }
}

// ================================================================================
// EventSystem/CommonEvents.cs
// ================================================================================

using System;
using System.Collections.Generic;

namespace ModSystem.Core
{
    /// <summary>
    /// 模组加载事件
    /// </summary>
    public class ModLoadedEvent : IModEvent
    {
        public string EventId => "mod_loaded";
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
        
        public string ModId { get; set; }
        public string ModName { get; set; }
        public string Version { get; set; }
    }
    
    /// <summary>
    /// 模组卸载事件
    /// </summary>
    public class ModUnloadedEvent : IModEvent
    {
        public string EventId => "mod_unloaded";
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
        
        public string ModId { get; set; }
        public string Reason { get; set; }
    }
    
    /// <summary>
    /// 模组错误事件
    /// </summary>
    public class ModErrorEvent : IModEvent
    {
        public string EventId => "mod_error";
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
        
        public string ErrorType { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }
    
    /// <summary>
    /// 交互事件
    /// </summary>
    public class InteractionEvent : IModEvent
    {
        public string EventId => "interaction";
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
        
        public string TargetId { get; set; }
        public InteractionType InteractionType { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }
    
    /// <summary>
    /// 交互类型枚举
    /// </summary>
    public enum InteractionType
    {
        Click,
        DoubleClick,
        Hover,
        Drag,
        Drop,
        Touch,
        Hold
    }
    
    /// <summary>
    /// 紧急停止事件
    /// </summary>
    public class EmergencyStopEvent : IModEvent
    {
        public string EventId => "emergency_stop";
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
        
        public string Reason { get; set; }
    }
}

// ================================================================================
// Communication/RequestResponse.cs
// ================================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace ModSystem.Core
{
    /// <summary>
    /// 模组请求基类
    /// </summary>
    public abstract class ModRequest : IModEvent
    {
        public string EventId => GetType().Name;
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// 请求的唯一标识符
        /// </summary>
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
    }
    
    /// <summary>
    /// 模组响应基类
    /// </summary>
    public abstract class ModResponse : IModEvent
    {
        public string EventId => GetType().Name;
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// 对应的请求ID
        /// </summary>
        public string RequestId { get; set; }
        
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// 响应消息
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// 响应结果
        /// </summary>
        public object Result { get; set; }
    }
    
    /// <summary>
    /// 请求响应管理器接口
    /// </summary>
    public interface IRequestResponseManager
    {
        /// <summary>
        /// 发送请求并等待响应
        /// </summary>
        /// <typeparam name="TRequest">请求类型</typeparam>
        /// <typeparam name="TResponse">响应类型</typeparam>
        /// <param name="request">请求对象</param>
        /// <param name="timeout">超时时间</param>
        /// <returns>响应对象</returns>
        Task<TResponse> SendRequestAsync<TRequest, TResponse>(
            TRequest request, 
            TimeSpan? timeout = null) 
            where TRequest : ModRequest 
            where TResponse : ModResponse;
    }
    
    /// <summary>
    /// 请求响应管理器实现
    /// </summary>
    public class RequestResponseManager : IRequestResponseManager, IDisposable
    {
        private readonly IEventBus eventBus;
        private readonly Dictionary<string, PendingRequest> pendingRequests;
        private readonly Timer cleanupTimer;
        private readonly object lockObject = new object();
        
        /// <summary>
        /// 待处理请求信息
        /// </summary>
        private class PendingRequest
        {
            public TaskCompletionSource<ModResponse> CompletionSource { get; set; }
            public Type ResponseType { get; set; }
            public DateTime CreatedAt { get; set; }
            public CancellationTokenSource CancellationTokenSource { get; set; }
        }
        
        /// <summary>
        /// 创建请求响应管理器
        /// </summary>
        /// <param name="eventBus">事件总线</param>
        public RequestResponseManager(IEventBus eventBus)
        {
            this.eventBus = eventBus;
            this.pendingRequests = new Dictionary<string, PendingRequest>();
            
            // 定期清理超时请求
            cleanupTimer = new Timer(CleanupTimeoutRequests, null, 
                TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }
        
        /// <summary>
        /// 发送请求并等待响应
        /// </summary>
        public async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
            TRequest request, 
            TimeSpan? timeout = null) 
            where TRequest : ModRequest 
            where TResponse : ModResponse
        {
            var actualTimeout = timeout ?? TimeSpan.FromSeconds(30);
            var cts = new CancellationTokenSource(actualTimeout);
            var tcs = new TaskCompletionSource<ModResponse>();
            
            // 注册取消回调
            cts.Token.Register(() =>
            {
                tcs.TrySetCanceled();
                CleanupRequest(request.RequestId);
            });
            
            lock (lockObject)
            {
                pendingRequests[request.RequestId] = new PendingRequest
                {
                    CompletionSource = tcs,
                    ResponseType = typeof(TResponse),
                    CreatedAt = DateTime.Now,
                    CancellationTokenSource = cts
                };
            }
            
            // 订阅响应事件
            Action<TResponse> responseHandler = null;
            responseHandler = (response) =>
            {
                if (response.RequestId == request.RequestId)
                {
                    lock (lockObject)
                    {
                        if (pendingRequests.TryGetValue(request.RequestId, out var pending))
                        {
                            pending.CompletionSource.TrySetResult(response);
                            CleanupRequest(request.RequestId);
                        }
                    }
                    eventBus.Unsubscribe(responseHandler);
                }
            };
            
            eventBus.Subscribe(responseHandler);
            eventBus.Publish(request);
            
            try
            {
                var result = await tcs.Task;
                return (TResponse)result;
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException($"Request {request.RequestId} timed out after {actualTimeout}");
            }
        }
        
        /// <summary>
        /// 清理请求
        /// </summary>
        private void CleanupRequest(string requestId)
        {
            lock (lockObject)
            {
                if (pendingRequests.TryGetValue(requestId, out var pending))
                {
                    pending.CancellationTokenSource?.Dispose();
                    pendingRequests.Remove(requestId);
                }
            }
        }
        
        /// <summary>
        /// 清理超时请求
        /// </summary>
        private void CleanupTimeoutRequests(object state)
        {
            var now = DateTime.Now;
            List<string> timeoutRequests;
            
            lock (lockObject)
            {
                timeoutRequests = pendingRequests
                    .Where(kvp => (now - kvp.Value.CreatedAt) > TimeSpan.FromMinutes(5))
                    .Select(kvp => kvp.Key)
                    .ToList();
            }
            
            foreach (var requestId in timeoutRequests)
            {
                lock (lockObject)
                {
                    if (pendingRequests.TryGetValue(requestId, out var pending))
                    {
                        pending.CompletionSource.TrySetException(
                            new TimeoutException("Request timed out during cleanup")
                        );
                        CleanupRequest(requestId);
                    }
                }
            }
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            cleanupTimer?.Dispose();
            
            // 取消所有待处理请求
            lock (lockObject)
            {
                foreach (var requestId in pendingRequests.Keys.ToList())
                {
                    CleanupRequest(requestId);
                }
            }
        }
    }
}

// ================================================================================
// Communication/CommunicationRouter.cs
// ================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Newtonsoft.Json;

namespace ModSystem.Core
{
    /// <summary>
    /// 通信配置
    /// </summary>
    public class CommunicationConfig
    {
        public List<RouteConfig> Routes { get; set; } = new List<RouteConfig>();
        public List<WorkflowConfig> Workflows { get; set; } = new List<WorkflowConfig>();
        public RouterSettings Settings { get; set; } = new RouterSettings();
    }
    
    /// <summary>
    /// 路由配置
    /// </summary>
    public class RouteConfig
    {
        public string Name { get; set; }
        public string SourceEvent { get; set; }
        public List<ConditionConfig> Conditions { get; set; } = new List<ConditionConfig>();
        public List<ActionConfig> Actions { get; set; } = new List<ActionConfig>();
        public bool Enabled { get; set; } = true;
        public int Priority { get; set; } = 0;
    }
    
    /// <summary>
    /// 条件配置
    /// </summary>
    public class ConditionConfig
    {
        public string Property { get; set; }
        public string Operator { get; set; }
        public object Value { get; set; }
    }
    
    /// <summary>
    /// 动作配置
    /// </summary>
    public class ActionConfig
    {
        public string TargetMod { get; set; }
        public string EventType { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public int Delay { get; set; }
    }
    
    /// <summary>
    /// 工作流配置
    /// </summary>
    public class WorkflowConfig
    {
        public string Name { get; set; }
        public TriggerConfig Trigger { get; set; }
        public List<WorkflowStep> Steps { get; set; }
    }
    
    /// <summary>
    /// 触发器配置
    /// </summary>
    public class TriggerConfig
    {
        public string Event { get; set; }
        public List<ConditionConfig> Conditions { get; set; }
    }
    
    /// <summary>
    /// 工作流步骤
    /// </summary>
    public class WorkflowStep
    {
        public string Action { get; set; }
        public string Event { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public int Delay { get; set; }
        public int Timeout { get; set; }
    }
    
    /// <summary>
    /// 路由器设置
    /// </summary>
    public class RouterSettings
    {
        public bool EnableDebugLogging { get; set; } = false;
        public int MaxConcurrentActions { get; set; } = 10;
        public int DefaultActionTimeout { get; set; } = 5000;
    }
    
    /// <summary>
    /// 通信路由器
    /// 根据配置自动路由事件
    /// </summary>
    public class CommunicationRouter
    {
        private readonly IEventBus eventBus;
        private readonly ILogger logger;
        private readonly CommunicationConfig config;
        private readonly Dictionary<string, List<RouteConfig>> routeMap;
        private readonly Dictionary<Type, MethodInfo> subscribeMethodCache;
        private readonly SemaphoreSlim actionSemaphore;
        
        /// <summary>
        /// 创建通信路由器
        /// </summary>
        /// <param name="eventBus">事件总线</param>
        /// <param name="configJson">配置JSON字符串</param>
        /// <param name="logger">日志记录器</param>
        public CommunicationRouter(IEventBus eventBus, string configJson, ILogger logger = null)
        {
            this.eventBus = eventBus;
            this.logger = logger;
            this.config = JsonConvert.DeserializeObject<CommunicationConfig>(configJson);
            this.routeMap = BuildRouteMap();
            this.subscribeMethodCache = new Dictionary<Type, MethodInfo>();
            this.actionSemaphore = new SemaphoreSlim(config.Settings.MaxConcurrentActions);
            
            SubscribeToEvents();
        }
        
        /// <summary>
        /// 构建路由映射
        /// </summary>
        private Dictionary<string, List<RouteConfig>> BuildRouteMap()
        {
            var map = new Dictionary<string, List<RouteConfig>>();
            
            foreach (var route in config.Routes.Where(r => r.Enabled))
            {
                if (!map.ContainsKey(route.SourceEvent))
                {
                    map[route.SourceEvent] = new List<RouteConfig>();
                }
                map[route.SourceEvent].Add(route);
            }
            
            // 按优先级排序
            foreach (var routes in map.Values)
            {
                routes.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }
            
            return map;
        }
        
        /// <summary>
        /// 订阅配置中的所有事件
        /// </summary>
        private void SubscribeToEvents()
        {
            foreach (var eventTypeName in routeMap.Keys)
            {
                try
                {
                    var eventType = GetTypeFromName(eventTypeName);
                    if (eventType == null)
                    {
                        logger?.LogError($"Event type not found: {eventTypeName}");
                        continue;
                    }
                    
                    var subscribeMethod = GetSubscribeMethod(eventType);
                    var handlerType = typeof(Action<>).MakeGenericType(eventType);
                    var handleMethod = GetType().GetMethod(nameof(HandleEvent), 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    var genericHandleMethod = handleMethod.MakeGenericMethod(eventType);
                    
                    var handler = Delegate.CreateDelegate(handlerType, this, genericHandleMethod);
                    
                    subscribeMethod.Invoke(eventBus, new[] { handler });
                    
                    logger?.Log($"Router subscribed to {eventTypeName}");
                }
                catch (Exception ex)
                {
                    logger?.LogError($"Failed to subscribe to {eventTypeName}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 获取订阅方法
        /// </summary>
        private MethodInfo GetSubscribeMethod(Type eventType)
        {
            if (!subscribeMethodCache.TryGetValue(eventType, out var method))
            {
                method = eventBus.GetType()
                    .GetMethod("Subscribe")
                    .MakeGenericMethod(eventType);
                subscribeMethodCache[eventType] = method;
            }
            return method;
        }
        
        /// <summary>
        /// 从名称获取类型
        /// </summary>
        private Type GetTypeFromName(string typeName)
        {
            // 首先尝试在所有已加载的程序集中查找
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null) return type;
            }
            
            // 尝试Type.GetType（支持程序集限定名）
            return Type.GetType(typeName);
        }
        
        /// <summary>
        /// 处理事件
        /// </summary>
        private async void HandleEvent<T>(T eventData) where T : IModEvent
        {
            var eventTypeName = typeof(T).FullName ?? typeof(T).Name;
            
            if (config.Settings.EnableDebugLogging)
            {
                logger?.Log($"Router handling event: {eventTypeName}");
            }
            
            if (routeMap.TryGetValue(eventTypeName, out var routes))
            {
                var tasks = new List<Task>();
                
                foreach (var route in routes)
                {
                    if (EvaluateConditions(route.Conditions, eventData))
                    {
                        tasks.Add(ExecuteActionsAsync(route, eventData));
                    }
                }
                
                if (tasks.Count > 0)
                {
                    try
                    {
                        await Task.WhenAll(tasks);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError($"Error executing route actions: {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 评估条件
        /// </summary>
        private bool EvaluateConditions<T>(List<ConditionConfig> conditions, T eventData)
        {
            if (conditions == null || conditions.Count == 0)
                return true;
            
            foreach (var condition in conditions)
            {
                var value = GetPropertyValue(eventData, condition.Property);
                
                if (!EvaluateCondition(value, condition.Operator, condition.Value))
                {
                    if (config.Settings.EnableDebugLogging)
                    {
                        logger?.Log($"Condition failed: {condition.Property} {condition.Operator} {condition.Value}");
                    }
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 获取属性值
        /// </summary>
        private object GetPropertyValue(object obj, string propertyPath)
        {
            var parts = propertyPath.Split('.');
            var current = obj;
            
            foreach (var part in parts)
            {
                if (current == null) return null;
                
                var prop = current.GetType().GetProperty(part);
                if (prop == null) return null;
                
                current = prop.GetValue(current);
            }
            
            return current;
        }
        
        /// <summary>
        /// 评估单个条件
        /// </summary>
        private bool EvaluateCondition(object value, string op, object expected)
        {
            switch (op)
            {
                case "==":
                    return Equals(value, expected);
                case "!=":
                    return !Equals(value, expected);
                case ">":
                    return Compare(value, expected) > 0;
                case ">=":
                    return Compare(value, expected) >= 0;
                case "<":
                    return Compare(value, expected) < 0;
                case "<=":
                    return Compare(value, expected) <= 0;
                case "contains":
                    return value?.ToString().Contains(expected?.ToString()) ?? false;
                case "startswith":
                    return value?.ToString().StartsWith(expected?.ToString()) ?? false;
                case "endswith":
                    return value?.ToString().EndsWith(expected?.ToString()) ?? false;
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// 比较两个值
        /// </summary>
        private int Compare(object a, object b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            
            if (a is IComparable comparable)
            {
                return comparable.CompareTo(b);
            }
            
            return string.Compare(a.ToString(), b.ToString());
        }
        
        /// <summary>
        /// 执行动作
        /// </summary>
        private async Task ExecuteActionsAsync<T>(RouteConfig route, T sourceEvent)
        {
            foreach (var action in route.Actions)
            {
                await actionSemaphore.WaitAsync();
                
                try
                {
                    if (action.Delay > 0)
                    {
                        await Task.Delay(action.Delay);
                    }
                    
                    var parameters = PrepareParameters(action.Parameters, sourceEvent);
                    var targetEvent = CreateEvent(action.EventType, parameters);
                    
                    if (targetEvent != null)
                    {
                        eventBus.Publish(targetEvent);
                        
                        if (config.Settings.EnableDebugLogging)
                        {
                            logger?.Log($"Route {route.Name} published {action.EventType}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError($"Failed to execute action in route {route.Name}: {ex.Message}");
                }
                finally
                {
                    actionSemaphore.Release();
                }
            }
        }
        
        /// <summary>
        /// 准备参数
        /// </summary>
        private Dictionary<string, object> PrepareParameters<T>(Dictionary<string, object> parameters, T sourceEvent)
        {
            if (parameters == null) return new Dictionary<string, object>();
            
            var result = new Dictionary<string, object>();
            
            foreach (var kvp in parameters)
            {
                if (kvp.Value is string str && str.StartsWith("${") && str.EndsWith("}"))
                {
                    // 处理变量引用
                    var path = str.Substring(2, str.Length - 3);
                    result[kvp.Key] = GetPropertyValue(sourceEvent, path);
                }
                else
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 创建事件实例
        /// </summary>
        private IModEvent CreateEvent(string eventTypeName, Dictionary<string, object> parameters)
        {
            try
            {
                var eventType = GetTypeFromName(eventTypeName);
                if (eventType == null)
                {
                    logger?.LogError($"Cannot create event: type not found {eventTypeName}");
                    return null;
                }
                
                var instance = Activator.CreateInstance(eventType) as IModEvent;
                
                // 设置参数
                foreach (var kvp in parameters)
                {
                    var prop = eventType.GetProperty(kvp.Key);
                    if (prop != null && prop.CanWrite)
                    {
                        var value = Convert.ChangeType(kvp.Value, prop.PropertyType);
                        prop.SetValue(instance, value);
                    }
                }
                
                return instance;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to create event {eventTypeName}: {ex.Message}");
                return null;
            }
        }
    }
}

// ================================================================================
// Services/ModServiceRegistry.cs
// ================================================================================

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
    
    /// <summary>
    /// 服务注册事件
    /// </summary>
    public class ServiceRegisteredEvent : IModEvent
    {
        public string EventId => "service_registered";
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
        
        public string ServiceType { get; set; }
        public string ServiceId { get; set; }
        public string ProviderId { get; set; }
        public string Version { get; set; }
    }
    
    /// <summary>
    /// 服务注销事件
    /// </summary>
    public class ServiceUnregisteredEvent : IModEvent
    {
        public string EventId => "service_unregistered";
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
        
        public string ServiceType { get; set; }
        public string ServiceId { get; set; }
        public string ProviderId { get; set; }
    }
}

// ================================================================================
// Runtime/ModManagerCore.cs
// ================================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace ModSystem.Core
{
    /// <summary>
    /// 平台无关的模组管理器核心
    /// 负责模组的加载、管理和生命周期控制
    /// </summary>
    public class ModManagerCore
    {
        private readonly ILogger logger;
        private readonly IPathProvider pathProvider;
        private readonly IEventBus eventBus;
        private readonly IServiceRegistry serviceRegistry;
        private readonly ModLoader modLoader;
        private readonly Dictionary<string, ModInstance> modInstances;
        private readonly SecurityManager securityManager;
        
        /// <summary>
        /// 最后的错误信息
        /// </summary>
        public string LastError { get; private set; }
        
        /// <summary>
        /// 验证错误列表
        /// </summary>
        public List<string> ValidationErrors { get; private set; } = new List<string>();
        
        /// <summary>
        /// 创建模组管理器核心
        /// </summary>
        public ModManagerCore(
            ILogger logger, 
            IPathProvider pathProvider,
            IEventBus eventBus,
            IServiceRegistry serviceRegistry)
        {
            this.logger = logger;
            this.pathProvider = pathProvider;
            this.eventBus = eventBus;
            this.serviceRegistry = serviceRegistry;
            
            var securityConfig = LoadSecurityConfig();
            securityManager = new SecurityManager(securityConfig, logger);
            
            modLoader = new ModLoader(logger, pathProvider, securityManager);
            modInstances = new Dictionary<string, ModInstance>();
        }
        
        /// <summary>
        /// 加载模组
        /// </summary>
        /// <param name="modPath">模组路径</param>
        /// <returns>是否成功加载</returns>
        public async Task<bool> LoadMod(string modPath)
        {
            try
            {
                ValidationErrors.Clear();
                
                // 加载模组
                var loadedMod = await modLoader.LoadModAsync(modPath);
                
                // 创建模组实例
                var instance = CreateModInstance(loadedMod);
                modInstances[loadedMod.Manifest.id] = instance;
                
                // 初始化行为
                foreach (var behaviour in loadedMod.Behaviours)
                {
                    var context = CreateModContext(loadedMod, instance);
                    behaviour.OnInitialize(context);
                }
                
                // 更新状态
                instance.State = ModState.Active;
                
                // 发布模组加载事件
                eventBus.Publish(new ModLoadedEvent
                {
                    ModId = loadedMod.Manifest.id,
                    ModName = loadedMod.Manifest.name,
                    Version = loadedMod.Manifest.version
                });
                
                logger.Log($"Mod loaded: {loadedMod.Manifest.name} v{loadedMod.Manifest.version}");
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                logger.LogError($"Failed to load mod: {ex}");
                return false;
            }
        }
        
        /// <summary>
        /// 从目录加载所有模组
        /// </summary>
        public async Task LoadModsFromDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                logger.LogWarning($"Mods directory not found: {directory}");
                return;
            }
            
            foreach (var modDir in Directory.GetDirectories(directory))
            {
                await LoadMod(modDir);
            }
        }
        
        /// <summary>
        /// 加载模组包文件
        /// </summary>
        public async Task<bool> LoadModPackage(string packagePath)
        {
            try
            {
                var tempPath = Path.Combine(pathProvider.GetTempPath(), Path.GetFileNameWithoutExtension(packagePath));
                
                // 解压包文件
                System.IO.Compression.ZipFile.ExtractToDirectory(packagePath, tempPath);
                
                // 加载模组
                var result = await LoadMod(tempPath);
                
                if (result)
                {
                    // 标记为临时模组
                    var manifest = await LoadManifest(tempPath);
                    if (modInstances.TryGetValue(manifest.id, out var instance))
                    {
                        instance.LoadedMod.IsTemporary = true;
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                logger.LogError($"Failed to load mod package: {ex}");
                return false;
            }
        }
        
        /// <summary>
        /// 从包目录加载所有模组包
        /// </summary>
        public async Task LoadModPackagesFromDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                logger.LogWarning($"Mod packages directory not found: {directory}");
                return;
            }
            
            foreach (var packageFile in Directory.GetFiles(directory, "*.modpack"))
            {
                await LoadModPackage(packageFile);
            }
        }
        
        /// <summary>
        /// 卸载模组
        /// </summary>
        public void UnloadMod(string modId)
        {
            if (modInstances.TryGetValue(modId, out var instance))
            {
                // 更新状态
                instance.State = ModState.Unloading;
                
                // 调用模组加载器卸载
                modLoader.UnloadMod(modId);
                
                // 移除实例
                modInstances.Remove(modId);
                
                // 发布卸载事件
                eventBus.Publish(new ModUnloadedEvent
                {
                    ModId = modId,
                    Reason = "Manual unload"
                });
            }
        }
        
        /// <summary>
        /// 获取模组实例
        /// </summary>
        public ModInstance GetModInstance(string modId)
        {
            return modInstances.TryGetValue(modId, out var instance) ? instance : null;
        }
        
        /// <summary>
        /// 获取所有已加载的模组
        /// </summary>
        public IEnumerable<ModInstance> GetLoadedMods()
        {
            return modInstances.Values;
        }
        
        /// <summary>
        /// 更新所有模组
        /// </summary>
        public void UpdateMods(float deltaTime)
        {
            foreach (var instance in modInstances.Values.Where(i => i.State == ModState.Active))
            {
                foreach (var behaviour in instance.LoadedMod.Behaviours)
                {
                    try
                    {
                        behaviour.OnUpdate(deltaTime);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error updating behaviour {behaviour.BehaviourId}: {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 创建模组实例
        /// </summary>
        private ModInstance CreateModInstance(LoadedMod loadedMod)
        {
            return new ModInstance
            {
                LoadedMod = loadedMod,
                State = ModState.Loaded,
                LoadTime = DateTime.Now
            };
        }
        
        /// <summary>
        /// 创建模组上下文
        /// </summary>
        private IModContext CreateModContext(LoadedMod loadedMod, ModInstance instance)
        {
            var securityContext = securityManager.CreateContext(
                loadedMod.Manifest.id, 
                loadedMod.Manifest.permissions
            );
            
            return new ModContext
            {
                ModId = loadedMod.Manifest.id,
                EventBus = eventBus,
                Services = serviceRegistry,
                Logger = logger,
                SecurityContext = securityContext,
                API = CreateModAPI(loadedMod)
            };
        }
        
        /// <summary>
        /// 创建模组API
        /// </summary>
        private IModAPI CreateModAPI(LoadedMod loadedMod)
        {
            return new ModAPI
            {
                RequestResponse = new RequestResponseManager(eventBus),
                ObjectFactory = null, // 需要在Unity层实现
                Utilities = new ModUtilities()
            };
        }
        
        /// <summary>
        /// 加载安全配置
        /// </summary>
        private SecurityConfig LoadSecurityConfig()
        {
            var configPath = Path.Combine(pathProvider.GetConfigPath(), "security_config.json");
            
            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<SecurityConfig>(json);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Failed to load security config: {ex.Message}");
                }
            }
            
            // 返回默认配置
            return new SecurityConfig();
        }
        
        /// <summary>
        /// 加载清单文件
        /// </summary>
        private async Task<ModManifest> LoadManifest(string modPath)
        {
            var manifestPath = Path.Combine(modPath, "manifest.json");
            var json = await File.ReadAllTextAsync(manifestPath);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<ModManifest>(json);
        }
    }
    
    /// <summary>
    /// 模组API实现
    /// </summary>
    internal class ModAPI : IModAPI
    {
        public IRequestResponseManager RequestResponse { get; set; }
        public IObjectFactory ObjectFactory { get; set; }
        public IModUtilities Utilities { get; set; }
    }
    
    /// <summary>
    /// 模组工具实现
    /// </summary>
    internal class ModUtilities : IModUtilities
    {
        public object StartCoroutine(System.Collections.IEnumerator enumerator)
        {
            // 需要在Unity层实现
            throw new NotImplementedException("Coroutines require Unity implementation");
        }
        
        public void StopCoroutine(object coroutine)
        {
            // 需要在Unity层实现
            throw new NotImplementedException("Coroutines require Unity implementation");
        }
    }
}

// ================================================================================
// Runtime/ModLoader.cs
// ================================================================================

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ModSystem.Core
{
    /// <summary>
    /// 平台无关的模组加载器
    /// 负责加载模组文件和程序集
    /// </summary>
    public class ModLoader
    {
        private readonly ILogger logger;
        private readonly IPathProvider pathProvider;
        private readonly SecurityManager securityManager;
        private readonly Dictionary<string, LoadedMod> loadedMods;
        
        /// <summary>
        /// 创建模组加载器
        /// </summary>
        public ModLoader(ILogger logger, IPathProvider pathProvider, SecurityManager securityManager = null)
        {
            this.logger = logger;
            this.pathProvider = pathProvider;
            this.securityManager = securityManager;
            this.loadedMods = new Dictionary<string, LoadedMod>();
        }
        
        /// <summary>
        /// 异步加载模组
        /// </summary>
        public async Task<LoadedMod> LoadModAsync(string modDirectory)
        {
            try
            {
                // 1. 加载清单文件
                var manifestPath = Path.Combine(modDirectory, "manifest.json");
                if (!File.Exists(manifestPath))
                {
                    throw new FileNotFoundException("Manifest file not found");
                }
                
                var manifestJson = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonConvert.DeserializeObject<ModManifest>(manifestJson);
                
                // 2. 验证安全性
                if (securityManager != null && !securityManager.ValidateMod(modDirectory))
                {
                    throw new SecurityException("Mod failed security validation");
                }
                
                // 3. 加载程序集
                Assembly assembly = null;
                var dllPath = Path.Combine(modDirectory, "Assemblies", $"{manifest.id}.dll");
                if (File.Exists(dllPath))
                {
                    assembly = Assembly.LoadFrom(dllPath);
                }
                
                // 4. 加载资源
                var resources = await LoadResourcesAsync(modDirectory, manifest);
                
                // 5. 创建模组实例
                var loadedMod = new LoadedMod
                {
                    Manifest = manifest,
                    Assembly = assembly,
                    Resources = resources,
                    RootPath = modDirectory
                };
                
                // 6. 实例化主模组行为类
                if (!string.IsNullOrEmpty(manifest.main_class) && assembly != null)
                {
                    var mainType = assembly.GetType(manifest.main_class);
                    if (mainType != null && typeof(IModBehaviour).IsAssignableFrom(mainType))
                    {
                        var behaviour = Activator.CreateInstance(mainType) as IModBehaviour;
                        loadedMod.Behaviours.Add(behaviour);
                    }
                }
                
                // 7. 加载额外的行为类
                if (manifest.behaviours != null && assembly != null)
                {
                    foreach (var behaviourClass in manifest.behaviours)
                    {
                        var behaviourType = assembly.GetType(behaviourClass);
                        if (behaviourType != null && typeof(IModBehaviour).IsAssignableFrom(behaviourType))
                        {
                            var behaviour = Activator.CreateInstance(behaviourType) as IModBehaviour;
                            loadedMod.Behaviours.Add(behaviour);
                        }
                    }
                }
                
                loadedMods[manifest.id] = loadedMod;
                logger.Log($"Mod loaded: {manifest.name} v{manifest.version}");
                
                return loadedMod;
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to load mod from {modDirectory}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 加载模组资源
        /// </summary>
        private async Task<ModResources> LoadResourcesAsync(string modDirectory, ModManifest manifest)
        {
            var resources = new ModResources();
            
            // 加载对象定义
            var objectsDir = Path.Combine(modDirectory, "Objects");
            if (Directory.Exists(objectsDir))
            {
                foreach (var objectFile in Directory.GetFiles(objectsDir, "*.json"))
                {
                    try
                    {
                        var objectJson = await File.ReadAllTextAsync(objectFile);
                        var objectDef = JsonConvert.DeserializeObject<ObjectDefinition>(objectJson);
                        resources.ObjectDefinitions[Path.GetFileName(objectFile)] = objectDef;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Failed to load object definition {objectFile}: {ex.Message}");
                    }
                }
            }
            
            // 加载配置文件
            var configDir = Path.Combine(modDirectory, "Config");
            if (Directory.Exists(configDir))
            {
                foreach (var configFile in Directory.GetFiles(configDir, "*.json"))
                {
                    try
                    {
                        var configData = await File.ReadAllTextAsync(configFile);
                        resources.Configs[Path.GetFileName(configFile)] = configData;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Failed to load config {configFile}: {ex.Message}");
                    }
                }
            }
            
            // 记录资源路径
            resources.ModelPaths = GetResourcePaths(modDirectory, "Models", "*.gltf", "*.glb");
            resources.TexturePaths = GetResourcePaths(modDirectory, "Resources/Textures", "*.png", "*.jpg");
            resources.AudioPaths = GetResourcePaths(modDirectory, "Resources/Audio", "*.wav", "*.mp3");
            
            return resources;
        }
        
        /// <summary>
        /// 获取资源路径
        /// </summary>
        private Dictionary<string, string> GetResourcePaths(string baseDir, string subDir, params string[] patterns)
        {
            var paths = new Dictionary<string, string>();
            var dir = Path.Combine(baseDir, subDir);
            
            if (Directory.Exists(dir))
            {
                foreach (var pattern in patterns)
                {
                    foreach (var file in Directory.GetFiles(dir, pattern))
                    {
                        paths[Path.GetFileName(file)] = file;
                    }
                }
            }
            
            return paths;
        }
        
        /// <summary>
        /// 卸载模组
        /// </summary>
        public void UnloadMod(string modId)
        {
            if (loadedMods.TryGetValue(modId, out var mod))
            {
                // 销毁所有行为
                foreach (var behaviour in mod.Behaviours)
                {
                    try
                    {
                        behaviour.OnDestroy();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error destroying behaviour: {ex.Message}");
                    }
                }
                
                // 清理临时文件
                if (mod.IsTemporary && Directory.Exists(mod.RootPath))
                {
                    try
                    {
                        Directory.Delete(mod.RootPath, true);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Failed to delete temporary files: {ex.Message}");
                    }
                }
                
                loadedMods.Remove(modId);
                logger.Log($"Mod {modId} unloaded");
            }
        }
        
        /// <summary>
        /// 获取已加载的模组
        /// </summary>
        public LoadedMod GetLoadedMod(string modId)
        {
            return loadedMods.TryGetValue(modId, out var mod) ? mod : null;
        }
    }
}

// ================================================================================
// Runtime/ObjectFactory.cs
// ================================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
                    if (value is Newtonsoft.Json.Linq.JArray jArray && typeof(T).IsArray)
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
    
    /// <summary>
    /// 抽象对象工厂基类
    /// </summary>
    public abstract class ObjectFactoryBase : IObjectFactory
    {
        protected readonly Dictionary<string, ObjectDefinition> definitionCache;
        protected readonly string basePath;
        protected readonly ILogger logger;
        
        /// <summary>
        /// 创建对象工厂
        /// </summary>
        protected ObjectFactoryBase(string basePath, ILogger logger)
        {
            this.basePath = basePath;
            this.logger = logger;
            definitionCache = new Dictionary<string, ObjectDefinition>();
        }
        
        /// <summary>
        /// 从定义文件创建对象
        /// </summary>
        public async Task<IGameObject> CreateObjectAsync(string definitionPath)
        {
            ObjectDefinition definition;
            
            if (definitionCache.ContainsKey(definitionPath))
            {
                definition = definitionCache[definitionPath];
            }
            else
            {
                var json = await LoadJsonAsync(definitionPath);
                definition = JsonConvert.DeserializeObject<ObjectDefinition>(json);
                definitionCache[definitionPath] = definition;
            }
            
            return await CreateObjectFromDefinitionAsync(definition);
        }
        
        /// <summary>
        /// 从对象定义创建对象（由子类实现）
        /// </summary>
        public abstract Task<IGameObject> CreateObjectFromDefinitionAsync(ObjectDefinition definition);
        
        /// <summary>
        /// 加载JSON文件
        /// </summary>
        protected virtual async Task<string> LoadJsonAsync(string path)
        {
            var fullPath = System.IO.Path.Combine(basePath, path);
            return await System.IO.File.ReadAllTextAsync(fullPath);
        }
        
        /// <summary>
        /// 配置对象行为
        /// </summary>
        protected virtual void ConfigureObjectBehaviour(IGameObject obj, ComponentDefinition compDef)
        {
            var behaviourClass = compDef.GetProperty<string>("behaviourClass");
            if (string.IsNullOrEmpty(behaviourClass))
            {
                logger.LogError("ObjectBehaviour requires behaviourClass property");
                return;
            }
            
            var behaviourType = Type.GetType(behaviourClass);
            if (behaviourType != null && typeof(IObjectBehaviour).IsAssignableFrom(behaviourType))
            {
                var behaviour = Activator.CreateInstance(behaviourType) as IObjectBehaviour;
                
                // 附加到对象
                behaviour.OnAttach(obj);
                
                // 配置行为
                var config = compDef.GetProperty<Dictionary<string, object>>("config");
                if (config != null)
                {
                    behaviour.OnConfigure(config);
                }
                
                // 存储引用
                var component = obj.AddComponent<ObjectBehaviourComponent>();
                if (component != null)
                {
                    component.Behaviour = behaviour;
                }
            }
            else
            {
                logger.LogError($"Could not find or instantiate behaviour class: {behaviourClass}");
            }
        }
    }
    
    /// <summary>
    /// 用于存储对象行为引用的组件
    /// </summary>
    public class ObjectBehaviourComponent
    {
        /// <summary>
        /// 关联的行为实例
        /// </summary>
        public IObjectBehaviour Behaviour { get; set; }
    }
}

// ================================================================================
// Runtime/ModContext.cs
// ================================================================================

namespace ModSystem.Core
{
    /// <summary>
    /// 模组上下文实现
    /// 为模组提供运行时环境
    /// </summary>
    internal class ModContext : IModContext
    {
        public string ModId { get; set; }
        public IGameObject GameObject { get; set; }
        public IEventBus EventBus { get; set; }
        public IModAPI API { get; set; }
        public IServiceRegistry Services { get; set; }
        public ISecurityContext SecurityContext { get; set; }
        public ILogger Logger { get; set; }
        
        /// <summary>
        /// 获取组件
        /// </summary>
        public T GetComponent<T>() where T : class
        {
            return GameObject?.GetComponent<T>();
        }
        
        /// <summary>
        /// 记录日志
        /// </summary>
        public void Log(string message)
        {
            Logger?.Log($"[{ModId}] {message}");
        }
        
        /// <summary>
        /// 记录错误
        /// </summary>
        public void LogError(string message)
        {
            Logger?.LogError($"[{ModId}] {message}");
        }
    }
}

// ================================================================================
// Runtime/ModInstance.cs
// ================================================================================

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
    
    /// <summary>
    /// 模组状态枚举
    /// </summary>
    public enum ModState
    {
        /// <summary>
        /// 未加载
        /// </summary>
        NotLoaded,
        
        /// <summary>
        /// 正在加载
        /// </summary>
        Loading,
        
        /// <summary>
        /// 已加载
        /// </summary>
        Loaded,
        
        /// <summary>
        /// 激活中
        /// </summary>
        Active,
        
        /// <summary>
        /// 已暂停
        /// </summary>
        Paused,
        
        /// <summary>
        /// 正在卸载
        /// </summary>
        Unloading,
        
        /// <summary>
        /// 错误状态
        /// </summary>
        Error
    }
}

// ================================================================================
// Runtime/LoadedMod.cs
// ================================================================================

using System.Collections.Generic;
using System.Reflection;

namespace ModSystem.Core
{
    /// <summary>
    /// 已加载的模组数据
    /// </summary>
    public class LoadedMod
    {
        /// <summary>
        /// 模组清单
        /// </summary>
        public ModManifest Manifest { get; set; }
        
        /// <summary>
        /// 模组程序集
        /// </summary>
        public Assembly Assembly { get; set; }
        
        /// <summary>
        /// 模组资源
        /// </summary>
        public ModResources Resources { get; set; }
        
        /// <summary>
        /// 模组行为列表
        /// </summary>
        public List<IModBehaviour> Behaviours { get; set; } = new List<IModBehaviour>();
        
        /// <summary>
        /// 模组根目录
        /// </summary>
        public string RootPath { get; set; }
        
        /// <summary>
        /// 是否为临时模组
        /// </summary>
        public bool IsTemporary { get; set; }
    }
}

// ================================================================================
// Runtime/ModManifest.cs
// ================================================================================

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
    
    /// <summary>
    /// 模组依赖项
    /// </summary>
    [Serializable]
    public class ModDependency
    {
        public string id { get; set; }
        public string version { get; set; }
        public bool optional { get; set; }
    }
    
    /// <summary>
    /// 服务定义
    /// </summary>
    [Serializable]
    public class ServiceDefinition
    {
        public string @interface { get; set; }
        public string implementation { get; set; }
    }
    
    /// <summary>
    /// 资源列表
    /// </summary>
    [Serializable]
    public class ResourceList
    {
        public string[] models { get; set; }
        public string[] objects { get; set; }
        public string[] configs { get; set; }
        public string[] textures { get; set; }
        public string[] audio { get; set; }
    }
    
    /// <summary>
    /// 模组元数据
    /// </summary>
    [Serializable]
    public class ModMetadata
    {
        public string[] tags { get; set; }
        public string category { get; set; }
        public string homepage { get; set; }
    }
}

// ================================================================================
// Runtime/ModResources.cs
// ================================================================================

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

// ================================================================================
// Security/SecurityManager.cs
// ================================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;
using System.Linq;

namespace ModSystem.Core
{
    /// <summary>
    /// 安全管理器
    /// 负责模组的安全验证和权限控制
    /// </summary>
    public class SecurityManager
    {
        private readonly SecurityConfig config;
        private readonly ILogger logger;
        private readonly HashSet<string> whitelistedPaths;
        private readonly HashSet<string> blacklistedAPIs;
        
        /// <summary>
        /// 创建安全管理器
        /// </summary>
        public SecurityManager(SecurityConfig config, ILogger logger)
        {
            this.config = config;
            this.logger = logger;
            this.whitelistedPaths = new HashSet<string>();
            this.blacklistedAPIs = InitializeBlacklistedAPIs();
        }
        
        /// <summary>
        /// 验证模组
        /// </summary>
        public bool ValidateMod(string modPath)
        {
            try
            {
                // 1. 检查路径安全性
                if (!IsPathSafe(modPath))
                {
                    logger.LogError($"Mod path is not safe: {modPath}");
                    return false;
                }
                
                // 2. 验证数字签名（如果启用）
                if (config.RequireSignedMods)
                {
                    if (!VerifySignature(modPath))
                    {
                        logger.LogError($"Mod signature verification failed: {modPath}");
                        return false;
                    }
                }
                
                // 3. 扫描恶意代码
                if (!ScanForMaliciousCode(modPath))
                {
                    logger.LogError($"Mod contains suspicious code: {modPath}");
                    return false;
                }
                
                // 4. 验证权限
                if (!ValidatePermissions(modPath))
                {
                    logger.LogError($"Mod requests unauthorized permissions: {modPath}");
                    return false;
                }
                
                logger.Log($"Mod validation passed: {modPath}");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"Security validation error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 检查路径安全性
        /// </summary>
        private bool IsPathSafe(string path)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                var allowedPaths = config.AllowedModPaths ?? new List<string> { config.ModDirectory };
                
                // 检查是否在允许的路径内
                bool isInAllowedPath = false;
                foreach (var allowedPath in allowedPaths)
                {
                    var fullAllowedPath = Path.GetFullPath(allowedPath);
                    if (fullPath.StartsWith(fullAllowedPath, StringComparison.OrdinalIgnoreCase))
                    {
                        isInAllowedPath = true;
                        break;
                    }
                }
                
                if (!isInAllowedPath)
                {
                    logger.LogError($"Path {fullPath} is not in allowed directories");
                    return false;
                }
                
                // 检查路径遍历攻击
                if (path.Contains("..") || path.Contains("~"))
                {
                    logger.LogError("Path contains traversal characters");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"Path validation error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 验证数字签名
        /// </summary>
        private bool VerifySignature(string modPath)
        {
            var signaturePath = Path.Combine(modPath, "signature.sig");
            if (!File.Exists(signaturePath))
            {
                logger.LogWarning($"Signature file not found: {signaturePath}");
                return false;
            }
            
            try
            {
                using (var rsa = new RSACryptoServiceProvider())
                {
                    // 加载公钥
                    if (!File.Exists(config.PublicKeyPath))
                    {
                        logger.LogError("Public key file not found");
                        return false;
                    }
                    
                    var publicKey = File.ReadAllText(config.PublicKeyPath);
                    rsa.FromXmlString(publicKey);
                    
                    // 计算清单文件哈希
                    var manifestPath = Path.Combine(modPath, "manifest.json");
                    if (!File.Exists(manifestPath))
                    {
                        logger.LogError("Manifest file not found for signature verification");
                        return false;
                    }
                    
                    var manifestData = File.ReadAllBytes(manifestPath);
                    using (var sha256 = SHA256.Create())
                    {
                        var hash = sha256.ComputeHash(manifestData);
                        var signature = File.ReadAllBytes(signaturePath);
                        
                        return rsa.VerifyHash(hash, CryptoConfig.MapNameToOID("SHA256"), signature);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Signature verification error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 扫描恶意代码
        /// </summary>
        private bool ScanForMaliciousCode(string modPath)
        {
            var dllFiles = Directory.GetFiles(modPath, "*.dll", SearchOption.AllDirectories);
            
            foreach (var dll in dllFiles)
            {
                if (!ScanAssembly(dll))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 扫描程序集
        /// </summary>
        private bool ScanAssembly(string assemblyPath)
        {
            try
            {
                // 使用ReflectionOnlyLoad避免执行代码
                var assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
                var assembly = Assembly.ReflectionOnlyLoad(assemblyName.FullName);
                
                foreach (var type in assembly.GetTypes())
                {
                    // 检查危险的基类
                    if (IsDangerousType(type))
                    {
                        logger.LogError($"Dangerous type detected: {type.FullName}");
                        return false;
                    }
                    
                    // 检查方法调用
                    foreach (var method in type.GetMethods(
                        BindingFlags.Public | BindingFlags.NonPublic | 
                        BindingFlags.Instance | BindingFlags.Static))
                    {
                        if (IsDangerousMethod(method))
                        {
                            logger.LogError($"Dangerous method detected: {method.Name} in {type.FullName}");
                            return false;
                        }
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"Assembly scan error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 检查是否为危险类型
        /// </summary>
        private bool IsDangerousType(Type type)
        {
            var dangerousTypes = new[]
            {
                "System.Diagnostics.Process",
                "System.IO.FileSystemWatcher",
                "System.Net.WebClient",
                "System.Net.Http.HttpClient",
                "Microsoft.Win32.Registry"
            };
            
            return dangerousTypes.Any(dt => 
                type.FullName == dt || 
                (type.BaseType != null && type.BaseType.FullName == dt));
        }
        
        /// <summary>
        /// 检查是否为危险方法
        /// </summary>
        private bool IsDangerousMethod(MethodInfo method)
        {
            var dangerousPatterns = new[]
            {
                "Process.Start",
                "File.Delete",
                "Directory.Delete",
                "Registry.",
                "Assembly.Load",
                "AppDomain.CreateDomain",
                "Marshal.GetDelegateForFunctionPointer"
            };
            
            var methodFullName = $"{method.DeclaringType?.Name}.{method.Name}";
            
            return dangerousPatterns.Any(pattern => 
                methodFullName.Contains(pattern));
        }
        
        /// <summary>
        /// 验证权限
        /// </summary>
        private bool ValidatePermissions(string modPath)
        {
            var manifestPath = Path.Combine(modPath, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                logger.LogError("Manifest file not found for permission validation");
                return false;
            }
            
            try
            {
                var manifestJson = File.ReadAllText(manifestPath);
                var manifest = Newtonsoft.Json.JsonConvert.DeserializeObject<ModManifest>(manifestJson);
                
                if (manifest.permissions == null || manifest.permissions.Length == 0)
                {
                    return true; // 没有请求特殊权限
                }
                
                // 检查每个请求的权限
                foreach (var permission in manifest.permissions)
                {
                    if (!config.AllowedPermissions.Contains(permission))
                    {
                        logger.LogError($"Unauthorized permission requested: {permission}");
                        return false;
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"Permission validation error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 初始化黑名单API
        /// </summary>
        private HashSet<string> InitializeBlacklistedAPIs()
        {
            return new HashSet<string>
            {
                "System.IO.File.Delete",
                "System.IO.Directory.Delete",
                "System.Diagnostics.Process.Start",
                "System.Net.WebClient",
                "System.Net.Http.HttpClient",
                "System.Reflection.Assembly.Load",
                "System.Reflection.Assembly.LoadFrom",
                "System.Reflection.Assembly.LoadFile",
                "System.AppDomain.CreateDomain",
                "System.Runtime.InteropServices.Marshal",
                "Microsoft.Win32.Registry",
                "System.Security.Cryptography",
                "System.Threading.Thread.Abort",
                "System.Environment.Exit"
            };
        }
        
        /// <summary>
        /// 创建安全上下文
        /// </summary>
        public SecurityContext CreateContext(string modId, string[] requestedPermissions)
        {
            var grantedPermissions = new HashSet<string>();
            
            // 检查模组是否在信任列表中
            if (config.TrustedMods?.Contains(modId) == true)
            {
                // 信任的模组获得所有请求的权限
                grantedPermissions = new HashSet<string>(requestedPermissions ?? new string[0]);
            }
            else if (config.ModPermissions?.TryGetValue(modId, out var allowedPermissions) == true)
            {
                // 只授予配置中允许的权限
                foreach (var permission in requestedPermissions ?? new string[0])
                {
                    if (allowedPermissions.Contains(permission))
                    {
                        grantedPermissions.Add(permission);
                    }
                }
            }
            else
            {
                // 使用默认权限集
                foreach (var permission in requestedPermissions ?? new string[0])
                {
                    if (config.DefaultPermissions?.Contains(permission) == true)
                    {
                        grantedPermissions.Add(permission);
                    }
                }
            }
            
            var resourceLimits = config.ModResourceLimits?.GetValueOrDefault(modId) ?? 
                                config.ModResourceLimits?.GetValueOrDefault("default") ?? 
                                new ResourceLimits();
            
            return new SecurityContext
            {
                ModId = modId,
                Permissions = grantedPermissions,
                ResourceLimits = resourceLimits
            };
        }
    }
}

// ================================================================================
// Security/SecurityConfig.cs
// ================================================================================

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
    
    /// <summary>
    /// 资源限制配置
    /// </summary>
    public class ResourceLimits
    {
        /// <summary>
        /// 最大内存使用（MB）
        /// </summary>
        public int MaxMemoryMB { get; set; } = 100;
        
        /// <summary>
        /// 最大CPU时间（毫秒）
        /// </summary>
        public int MaxCpuTimeMs { get; set; } = 10;
        
        /// <summary>
        /// 最大对象数量
        /// </summary>
        public int MaxObjects { get; set; } = 50;
        
        /// <summary>
        /// 最大文件大小（字节）
        /// </summary>
        public int MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10MB
        
        /// <summary>
        /// 最大事件发送速率（每秒）
        /// </summary>
        public int MaxEventRate { get; set; } = 100;
        
        /// <summary>
        /// 最大服务调用数（每分钟）
        /// </summary>
        public int MaxServiceCalls { get; set; } = 1000;
    }
}

// ================================================================================
// Security/SecurityContext.cs
// ================================================================================

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
    
    /// <summary>
    /// 安全异常
    /// </summary>
    public class SecurityException : System.Exception
    {
        public SecurityException(string message) : base(message) { }
        public SecurityException(string message, System.Exception inner) : base(message, inner) { }
    }
}

// ================================================================================
// ModSystem.Core.csproj
// ================================================================================
/*
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net8.0;netstandard2.1</TargetFrameworks>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageId>ModSystem.Core</PackageId>
    <Version>1.0.0</Version>
    <Authors>Your Name</Authors>
    <Description>Platform-independent core library for Unity Mod System</Description>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <RepositoryUrl>https://github.com/your-repo/unity-mod-system</RepositoryUrl>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="System.Threading.Tasks.Extensions" Version="4.5.4" />
  </ItemGroup>

  <ItemGroup Condition="'$(TargetFramework)' == 'netstandard2.1'">
    <PackageReference Include="System.Memory" Version="4.5.5" />
  </ItemGroup>

</Project>
*/