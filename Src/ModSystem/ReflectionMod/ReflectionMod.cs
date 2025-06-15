using System;
using ModSystem.Core.Runtime;
using ModSystem.Core.Events;
using ModSystem.Core.Reflection;

namespace ReflectionMod
{
    /// <summary>
    /// 反射示例模组 - 演示使用ReflectionHelper动态创建Unity对象
    /// </summary>
    public class ReflectionMod : ModBase
    {
        public override string ModId => "reflection_mod";

        private object _createdCube;
        private object _createdLight;

        protected override void OnInitialize()
        {
            Logger.Log("ReflectionMod initialized!");

            // 订阅按钮事件
            Subscribe<ButtonClickedEvent>(OnButtonClicked);

            // 创建UI
            PublishEvent(new CreateUIRequestEvent
            {
                UIType = "ReflectionPanel",
                Title = "Reflection Mod",
                Buttons = new[]
                {
                    new ButtonConfig { Id = "create_cube", Text = "Create Cube" },
                    new ButtonConfig { Id = "create_light", Text = "Create Light" },
                    new ButtonConfig { Id = "change_color", Text = "Change Color" },
                    new ButtonConfig { Id = "rotate", Text = "Rotate" },
                    new ButtonConfig { Id = "cleanup", Text = "Clean Up" }
                }
            });
        }

        private void OnButtonClicked(ButtonClickedEvent e)
        {
            switch (e.ButtonId)
            {
                case "create_cube":
                    CreateCube();
                    break;
                case "create_light":
                    CreateLight();
                    break;
                case "change_color":
                    ChangeColor();
                    break;
                case "rotate":
                    RotateCube();
                    break;
                case "cleanup":
                    CleanUp();
                    break;
            }
        }

        private void CreateCube()
        {
            try
            {
                // 使用 GameObject.CreatePrimitive 创建立方体
                var primitiveType = ReflectionHelper.FindType("UnityEngine.PrimitiveType");
                if (primitiveType != null)
                {
                    var cubeValue = Enum.Parse(primitiveType, "Cube");
                    _createdCube = ReflectionHelper.InvokeStatic("UnityEngine.GameObject", "CreatePrimitive", cubeValue);

                    if (_createdCube != null)
                    {
                        // 设置名称
                        ReflectionHelper.SetProperty(_createdCube, "name", "ReflectionCube");

                        // 设置位置
                        var transform = ReflectionHelper.GetProperty(_createdCube, "transform");
                        if (transform != null)
                        {
                            var vector3Type = ReflectionHelper.FindType("UnityEngine.Vector3");
                            var position = Activator.CreateInstance(vector3Type, 0f, 2f, 0f);
                            ReflectionHelper.SetProperty(transform, "position", position);
                        }

                        Logger.Log("Cube created successfully!");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to create cube: {ex.Message}");
            }
        }

        private void CreateLight()
        {
            try
            {
                _createdLight = ReflectionHelper.CreateGameObject("ReflectionLight");
                if (_createdLight == null) return;

                var light = ReflectionHelper.AddComponent(_createdLight, "UnityEngine.Light");
                if (light != null)
                {
                    ReflectionHelper.SetProperty(light, "intensity", 2.0f);
                    ReflectionHelper.SetProperty(light, "range", 20.0f);

                    // 设置光源类型为点光源
                    var lightType = ReflectionHelper.FindType("UnityEngine.LightType");
                    if (lightType != null)
                    {
                        var pointLight = Enum.Parse(lightType, "Point");
                        ReflectionHelper.SetProperty(light, "type", pointLight);
                    }
                }

                var transform = ReflectionHelper.GetProperty(_createdLight, "transform");
                if (transform != null)
                {
                    var vector3Type = ReflectionHelper.FindType("UnityEngine.Vector3");
                    var position = Activator.CreateInstance(vector3Type, 0f, 5f, 0f);
                    ReflectionHelper.SetProperty(transform, "position", position);

                    // 设置旋转
                    var rotation = Activator.CreateInstance(vector3Type, 45f, -30f, 0f);
                    ReflectionHelper.SetProperty(transform, "eulerAngles", rotation);
                }

                Logger.Log("Light created successfully!");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to create light: {ex.Message}");
            }
        }

        private void ChangeColor()
        {
            if (_createdCube == null)
            {
                Logger.LogWarning("No cube to change color!");
                return;
            }

            try
            {
                var renderer = ReflectionHelper.GetComponent(_createdCube, "UnityEngine.MeshRenderer");
                if (renderer != null)
                {
                    var material = ReflectionHelper.GetProperty(renderer, "material");
                    if (material != null)
                    {
                        var colorType = ReflectionHelper.FindType("UnityEngine.Color");
                        var r = (float)new Random().NextDouble();
                        var g = (float)new Random().NextDouble();
                        var b = (float)new Random().NextDouble();
                        var color = Activator.CreateInstance(colorType, r, g, b, 1.0f);

                        ReflectionHelper.SetProperty(material, "color", color);
                        Logger.Log($"Changed color to RGB({r:F2}, {g:F2}, {b:F2})");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to change color: {ex.Message}");
            }
        }

        private void RotateCube()
        {
            if (_createdCube == null)
            {
                Logger.LogWarning("No cube to rotate!");
                return;
            }

            try
            {
                var transform = ReflectionHelper.GetProperty(_createdCube, "transform");
                if (transform != null)
                {
                    ReflectionHelper.InvokeMethod(transform, "Rotate", 0f, 45f, 0f);
                    Logger.Log("Rotated cube by 45 degrees");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to rotate: {ex.Message}");
            }
        }

        private void CleanUp()
        {
            if (_createdCube != null)
            {
                ReflectionHelper.Destroy(_createdCube);
                _createdCube = null;
                Logger.Log("Cube destroyed");
            }

            if (_createdLight != null)
            {
                ReflectionHelper.Destroy(_createdLight);
                _createdLight = null;
                Logger.Log("Light destroyed");
            }
        }

        protected override void OnShutdown()
        {
            CleanUp();
        }
    }
}