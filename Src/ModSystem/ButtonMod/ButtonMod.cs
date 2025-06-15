using System;
using System.Collections.Generic;
using ModSystem.Core.Runtime;
using ModSystem.Core.Events;

namespace ButtonMod
{
    public class ButtonMod : ModBase
    {
        public override string ModId => "button_mod";
        private int _clickCount = 0;

        protected override void OnInitialize()
        {
            Subscribe<ButtonClickedEvent>(OnButtonClicked);

            // 创建UI
            PublishEvent(new CreateUIRequestEvent
            {
                UIType = "ButtonPanel",
                Title = "Button Mod",
                Buttons = new[]
                {
                    new ButtonConfig { Id = "test", Text = "Click Me!" },
                    new ButtonConfig { Id = "broadcast", Text = "Broadcast" }
                }
            });
        }

        private void OnButtonClicked(ButtonClickedEvent e)
        {
            Logger.Log($"ButtonMod received click: {e.ButtonId}");  // 添加调试日志

            if (e.ButtonId == "test")
            {
                _clickCount++;
                Logger.Log($"Clicked {_clickCount} times");
            }
            else if (e.ButtonId == "broadcast")
            {
                PublishEvent(new BroadcastEvent
                {
                    Message = $"Hello! (clicks: {_clickCount})",
                    Data = new Dictionary<string, object> { { "count", _clickCount } }
                });
            }
        }
    }
}