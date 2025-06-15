using ModSystem.Core.Interfaces;

namespace ModSystem.Core.Runtime
{
    /// <summary>
    /// 模组基类（可选使用）
    /// </summary>
    public abstract class ModBase : IModBehaviour
    {
        protected ILogger Logger { get; private set; }

        public abstract string ModId { get; }

        // 无参构造函数
        protected ModBase()
        {
            Logger = new NullLogger();
        }

        // 带logger的构造函数
        protected ModBase(ILogger logger)
        {
            Logger = logger ?? new NullLogger();
        }

        // 设置Logger（由ModManager调用）
        public void SetLogger(ILogger logger)
        {
            Logger = logger ?? new NullLogger();
        }

        public abstract void Initialize();
        public abstract void Shutdown();

        // 空日志实现
        private class NullLogger : ILogger
        {
            public void Log(string message) { }
            public void LogWarning(string message) { }
            public void LogError(string message) { }
        }
    }
}