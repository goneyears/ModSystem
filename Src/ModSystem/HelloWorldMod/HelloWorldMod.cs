using System;
using ModSystem.Core.Runtime;

namespace HelloWorldMod
{
    /// <summary>
    /// 使用ModBase的示例模组
    /// </summary>
    public class HelloWorldModSimple : ModBase
    {
        public override string ModId => "hello_world_mod_simple";

        public override void Initialize()
        {
            Logger.Log($"[{ModId}] Hello from ModBase! Initialized at {DateTime.Now}");
        }

        public override void Shutdown()
        {
            Logger.Log($"[{ModId}] Goodbye from ModBase! Shutdown at {DateTime.Now}");
        }
    }
}
