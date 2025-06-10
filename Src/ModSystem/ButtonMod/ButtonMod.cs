using ExampleMods;
using ModSystem.Core;
using System.Drawing;

// ===================================================================
// ButtonMod.cs - 简化版本，基于可工作的反射版本
// ===================================================================

using ModSystem.Core;
using System;

namespace ExampleMods
{
    /// <summary>
    /// 按钮模组 - 简化版本
    /// </summary>
    public class ButtonMod : ModBase
    {
        private string buttonId;
        private int clickCount = 0;

        // 颜色定义 - 和原版一样
        private readonly string[] colors = { "#FFFFFF", "#FF0000", "#00FF00", "#0000FF", "#FF00FF" };

        public override void OnInitialize(IModContext context)
        {
            base.OnInitialize(context);
            Context.Log("[ButtonMod] Initialized");
        }

        public override void OnEnable()
        {
            Context.Log("[ButtonMod] OnEnable called - starting UI creation");
            base.OnEnable();

            // 创建按钮
            buttonId = Context.UI.CreateButton("ModButton", "ccccClick Meeee!", OnButtonClick);

            // 设置按钮属性 - 使用和原版类似的尺寸
            Context.UI.SetProperty(buttonId, "size", (30f, 30f));
            Context.UI.SetProperty(buttonId, "position", (10f, 0f));

            Context.Log("Button mod is ready!!!!");
        }

        // 这个方法会被按钮调用
        public void OnButtonClick()
        {
            clickCount++;
            Context.Log($"Button clicked {clickCount} times");

            Context.UI.SetProperty(buttonId, "size", (130f, 30f));
            // 改变按钮颜色
            string color = colors[clickCount % colors.Length];
            Context.UI.SetProperty(buttonId, "color", color);

            Context.Log($"Button color changed to: {color}");
        }

        public override void OnDestroy()
        {
            Context.Log("[ButtonMod] OnDestroy called");
            base.OnDestroy();
        }
    }
}


