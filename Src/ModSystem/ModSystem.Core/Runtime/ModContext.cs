using ModSystem.Core.Interfaces;
using ModSystem.Core.Lifecycle;

namespace ModSystem.Core.Runtime
{
    /// <summary>
    /// 模组上下文，提供核心服务访问 - V5版本
    /// </summary>
    public class ModContext
    {
        public IEventBus EventBus { get; }
        public ILogger Logger { get; }
        public IUnityAccess UnityAccess { get; }

        // V4添加：生命周期管理器
        public LifecycleManager LifecycleManager { get; }

        // V5添加：配置文件路径
        public string ConfigPath { get; }

        public ModContext(
            IEventBus eventBus,
            ILogger logger,
            IUnityAccess unityAccess = null,
            LifecycleManager lifecycleManager = null,
            string configPath = null)  // V5添加参数
        {
            EventBus = eventBus;
            Logger = logger;
            UnityAccess = unityAccess;
            LifecycleManager = lifecycleManager;
            ConfigPath = configPath;
        }
    }
}