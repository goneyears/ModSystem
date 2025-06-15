using System;
using ModSystem.Core.Runtime;
using ModSystem.Core.Events;
using ModSystem.Core.Unity;
using ModSystem.Core.Reflection;

namespace ReflectionMod
{
    /// <summary>
    /// 简化版反射示例模组 - 使用UnityHelper
    /// </summary>
    public class SimpleReflectionMod : ModBase
    {
        public override string ModId => "simple_reflection_mod";

        private object _createdCube;
        private object _createdLight;
        private int _colorIndex = 0;
        private readonly float[,] _colors = new float[,]
        {
            { 1, 0, 0 },    // 红
            { 0, 1, 0 },    // 绿
            { 0, 0, 1 },    // 蓝
            { 1, 1, 0 },    // 黄
            { 1, 0, 1 },    // 品红
            { 0, 1, 1 }     // 青
        };

        protected override void OnInitialize()
        {
            Logger.Log("Simple Reflection Mod initialized!");

            // 订阅按钮事件
            Subscribe<ButtonClickedEvent>(OnButtonClicked);

            // 创建UI
            PublishEvent(new CreateUIRequestEvent
            {
                UIType = "SimpleReflectionPanel",
                Title = "Simple Reflection",
                Buttons = new[]
                {
                    new ButtonConfig { Id = "create_scene", Text = "Create Scene" },
                    new ButtonConfig { Id = "change_color", Text = "Change Color" },
                    new ButtonConfig { Id = "animate", Text = "Animate" },
                    new ButtonConfig { Id = "cleanup", Text = "Clean Up" }
                }
            });
        }

        private void OnButtonClicked(ButtonClickedEvent e)
        {
            switch (e.ButtonId)
            {
                case "create_scene":
                    CreateScene();
                    break;
                case "change_color":
                    ChangeColors();
                    break;
                case "animate":
                    AnimateObjects();
                    break;
                case "cleanup":
                    CleanUp();
                    break;
            }
        }

        private void CreateScene()
        {
            try
            {
                // 创建立方体
                _createdCube = UnityHelper.CreateCube("ColorfulCube");
                UnityHelper.SetPosition(_createdCube, 0, 1, 0);
                UnityHelper.SetColor(_createdCube, 1, 0, 0); // 红色

                // 创建点光源
                _createdLight = UnityHelper.CreatePointLight("MainLight", 2.0f, 20.0f);
                UnityHelper.SetPosition(_createdLight, 2, 4, -2);

                // 创建地面
                var ground = UnityHelper.CreatePlane("Ground");
                UnityHelper.SetScale(ground, 2, 1, 2);
                UnityHelper.SetColor(ground, 0.5f, 0.5f, 0.5f);

                Logger.Log("Scene created successfully!");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to create scene: {ex.Message}");
            }
        }

        private void ChangeColors()
        {
            if (_createdCube == null)
            {
                Logger.LogWarning("No cube to change color! Create scene first.");
                return;
            }

            try
            {
                // 循环切换颜色
                var r = _colors[_colorIndex, 0];
                var g = _colors[_colorIndex, 1];
                var b = _colors[_colorIndex, 2];

                UnityHelper.SetColor(_createdCube, r, g, b);

                _colorIndex = (_colorIndex + 1) % _colors.GetLength(0);
                Logger.Log($"Changed color to RGB({r}, {g}, {b})");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to change color: {ex.Message}");
            }
        }

        private void AnimateObjects()
        {
            if (_createdCube == null || _createdLight == null)
            {
                Logger.LogWarning("Create scene first!");
                return;
            }

            try
            {
                // 旋转立方体
                var cubeTransform = ReflectionHelper.GetProperty(_createdCube, "transform");
                if (cubeTransform != null)
                {
                    ReflectionHelper.InvokeMethod(cubeTransform, "Rotate", 0f, 45f, 0f);
                }

                // 移动光源（围绕立方体旋转）
                var time = DateTime.Now.Ticks / 10000000f;
                var x = (float)Math.Sin(time) * 3;
                var z = (float)Math.Cos(time) * 3;
                UnityHelper.SetPosition(_createdLight, x, 4, z);

                Logger.Log("Objects animated!");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to animate: {ex.Message}");
            }
        }

        private void CleanUp()
        {
            // 清理所有创建的对象
            var allObjects = ReflectionHelper.InvokeStatic("UnityEngine.GameObject", "FindObjectsOfType",
                ReflectionHelper.FindType("UnityEngine.GameObject"));

            if (allObjects is object[] gameObjects)
            {
                foreach (var obj in gameObjects)
                {
                    var name = ReflectionHelper.GetProperty(obj, "name") as string;
                    if (name != null && (name.Contains("Cube") || name.Contains("Light") || name == "Ground"))
                    {
                        ReflectionHelper.Destroy(obj);
                        Logger.Log($"Destroyed: {name}");
                    }
                }
            }

            _createdCube = null;
            _createdLight = null;
            _colorIndex = 0;
        }

        protected override void OnShutdown()
        {
            CleanUp();
        }
    }
}