using ModSystem.Core.Runtime;
using ModSystem.Core.Events;

namespace HelloWorldMod
{
    public class HelloWorldModV2 : ModBase
    {
        public override string ModId => "hello_world_v2";

        protected override void OnInitialize()
        {
            Logger.Log("Hello World V2!");
            Subscribe<BroadcastEvent>(OnBroadcast);
        }

        private void OnBroadcast(BroadcastEvent e)
        {
            if (e.SenderId != ModId)
            {
                Logger.Log($"Received: {e.Message} from {e.SenderId}");
            }
        }
    }
}