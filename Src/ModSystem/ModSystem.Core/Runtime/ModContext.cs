using ModSystem.Core.Interfaces;
using ModSystem.Core.Lifecycle;

namespace ModSystem.Core.Runtime
{
    /// <summary>
    /// 模组上下文，提供核心服务访问
    /// </summary>
    public class ModContext
    {
        public IEventBus EventBus { get; }
        public ILogger Logger { get; }
        public IUnityAccess UnityAccess { get; }

        // V4新增：生命周期管理器
        public LifecycleManager LifecycleManager { get; }

        public ModContext(IEventBus eventBus, ILogger logger, IUnityAccess unityAccess = null, LifecycleManager lifecycleManager = null)
        {
            EventBus = eventBus;
            Logger = logger;
            UnityAccess = unityAccess;
            LifecycleManager = lifecycleManager;
        }
    }
}