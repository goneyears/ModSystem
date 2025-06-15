using ModSystem.Core.Interfaces;

namespace ModSystem.Core.Runtime
{
    /// <summary>
    /// 模组上下文，提供核心服务访问
    /// </summary>
    public class ModContext
    {
        public IEventBus EventBus { get; }
        public ILogger Logger { get; }

        public ModContext(IEventBus eventBus, ILogger logger)
        {
            EventBus = eventBus;
            Logger = logger;
        }
    }
}