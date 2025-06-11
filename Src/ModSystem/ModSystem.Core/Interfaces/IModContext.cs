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
} 