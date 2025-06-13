namespace ModSystem.Core
{
    /// <summary>
    /// 模组上下文实现
    /// 为模组提供运行时环境
    /// </summary>
    public class ModContext : IModContext
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

        /// <summary>
        /// UI工厂（可选，Unity层特有）
        /// </summary>
        public object UIFactory { get; set; }

        /// <summary>
        /// 扩展数据存储
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object> ExtensionData { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ModContext()
        {
            ExtensionData = new System.Collections.Generic.Dictionary<string, object>();
        }

        /// <summary>
        /// 获取扩展数据
        /// </summary>
        public T GetExtension<T>(string key) where T : class
        {
            if (ExtensionData.TryGetValue(key, out var value))
            {
                return value as T;
            }
            return null;
        }

        /// <summary>
        /// 设置扩展数据
        /// </summary>
        public void SetExtension(string key, object value)
        {
            ExtensionData[key] = value;
        }
    }
} 