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