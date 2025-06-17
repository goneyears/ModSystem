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
        private object _createdGround;
        private int _colorIndex = 0;
        private float _rotationAngle = 0f;
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
                // 先清理可能存在的旧对象
                if (_createdCube != null || _createdLight != null || _createdGround != null)
                {
                    Logger.Log("Cleaning up existing scene...");
                    CleanUp();
                }

                // 创建立方体
                _createdCube = UnityHelper.CreateCube("ColorfulCube");
                UnityHelper.SetPosition(_createdCube, 0, 1, 0);
                UnityHelper.SetColor(_createdCube, 1, 0, 0); // 红色

                // 创建点光源
                _createdLight = UnityHelper.CreatePointLight("MainLight", 2.0f, 20.0f);
                UnityHelper.SetPosition(_createdLight, 2, 4, -2);

                // 创建地面
                _createdGround = UnityHelper.CreatePlane("Ground");
                UnityHelper.SetScale(_createdGround, 2, 1, 2);
                UnityHelper.SetColor(_createdGround, 0.5f, 0.5f, 0.5f);

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
                // 旋转立方体 - 使用UnityHelper
                UnityHelper.Rotate(_createdCube, 0, 45, 0);

                // 移动光源（围绕立方体旋转）
                _rotationAngle += 45f; // 每次增加45度
                var radians = _rotationAngle * (float)(Math.PI / 180);
                var x = (float)Math.Sin(radians) * 3;
                var z = (float)Math.Cos(radians) * 3;
                UnityHelper.SetPosition(_createdLight, x, 4, z);

                Logger.Log($"Objects animated! Light angle: {_rotationAngle % 360}°");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to animate: {ex.Message}");
            }
        }

        private void CleanUp()
        {
            try
            {
                // 清理立方体
                if (_createdCube != null)
                {
                    ReflectionHelper.Destroy(_createdCube);
                    _createdCube = null;
                    Logger.Log("Destroyed: Cube");
                }

                // 清理灯光
                if (_createdLight != null)
                {
                    ReflectionHelper.Destroy(_createdLight);
                    _createdLight = null;
                    Logger.Log("Destroyed: Light");
                }

                // 清理地面
                if (_createdGround != null)
                {
                    ReflectionHelper.Destroy(_createdGround);
                    _createdGround = null;
                    Logger.Log("Destroyed: Ground");
                }

                // 查找并清理可能遗留的对象（以防引用丢失）
                var remainingCube = ReflectionHelper.FindGameObject("ColorfulCube");
                if (remainingCube != null)
                {
                    ReflectionHelper.Destroy(remainingCube);
                    Logger.Log("Destroyed remaining: ColorfulCube");
                }

                var remainingLight = ReflectionHelper.FindGameObject("MainLight");
                if (remainingLight != null)
                {
                    ReflectionHelper.Destroy(remainingLight);
                    Logger.Log("Destroyed remaining: MainLight");
                }

                var remainingGround = ReflectionHelper.FindGameObject("Ground");
                if (remainingGround != null)
                {
                    ReflectionHelper.Destroy(remainingGround);
                    Logger.Log("Destroyed remaining: Ground");
                }

                _colorIndex = 0;
                _rotationAngle = 0f;
                Logger.Log("Cleanup completed!");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed during cleanup: {ex.Message}");
            }
        }

        protected override void OnShutdown()
        {
            CleanUp();
        }
    }
}