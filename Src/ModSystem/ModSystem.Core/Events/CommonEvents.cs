using System.Collections.Generic;
using ModSystem.Core.EventSystem;
using ModSystem.Core.Interfaces;

namespace ModSystem.Core.Events
{
    // 系统事件
    public class SystemReadyEvent : IModEvent
    {
        public string EventId => "system_ready";
    }

    public class ModLoadedEvent : ModEventBase
    {
        public override string EventId => "mod_loaded";
        public string LoadedModId { get; set; }
    }

    // UI事件
    public class CreateUIRequestEvent : ModEventBase
    {
        public override string EventId => "create_ui";
        public string UIType { get; set; }
        public string Title { get; set; }
        public ButtonConfig[] Buttons { get; set; }
    }

    public class ButtonClickedEvent : ModEventBase
    {
        public override string EventId => "button_clicked";
        public string ButtonId { get; set; }
    }

    public class ButtonConfig
    {
        public string Id { get; set; }
        public string Text { get; set; }
    }

    // 通信事件
    public class BroadcastEvent : ModEventBase
    {
        public override string EventId => "broadcast";
        public string Message { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }
}