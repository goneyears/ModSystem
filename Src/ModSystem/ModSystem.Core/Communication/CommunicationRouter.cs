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