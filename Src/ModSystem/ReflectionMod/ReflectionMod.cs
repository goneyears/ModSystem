using System;
using System.Collections.Generic;
using ModSystem.Core.Runtime;
using ModSystem.Core.Events;
using ModSystem.Core.Reflection;

namespace ReflectionMod
{
    /// <summary>
    /// 反射示例模组 - 支持创建多个对象的版本
    /// </summary>
    public class ReflectionModMultiple : ModBase
    {
        public override string ModId => "reflection_mod_multiple";

        private readonly List<object> _createdObjects = new List<object>();
        private readonly Random _random = new Random();

        protected override void OnInitialize()
        {
            Logger.Log("ReflectionMod (Multiple Objects) initialized!");

            // 订阅按钮事件
            Subscribe<ButtonClickedEvent>(OnButtonClicked);

            // 创建UI
            PublishEvent(new CreateUIRequestEvent
            {
                UIType = "ReflectionPanelMultiple",
                Title = "Reflection Mod (Multiple)",
                Buttons = new[]
                {
                    new ButtonConfig { Id = "create_cube", Text = "Add Cube" },
                    new ButtonConfig { Id = "create_light", Text = "Add Light" },
                    new ButtonConfig { Id = "change_colors", Text = "Change All Colors" },
                    new ButtonConfig { Id = "rotate_all", Text = "Rotate All" },
                    new ButtonConfig { Id = "cleanup", Text = "Clean Up All" }
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
                case "change_colors":
                    ChangeAllColors();
                    break;
                case "rotate_all":
                    RotateAll();
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
                var primitiveType = ReflectionHelper.FindType("UnityEngine.PrimitiveType");
                if (primitiveType != null)
                {
                    var cubeValue = Enum.Parse(primitiveType, "Cube");
                    var cube = ReflectionHelper.InvokeStatic("UnityEngine.GameObject", "CreatePrimitive", cubeValue);

                    if (cube != null)
                    {
                        // 设置唯一名称
                        var cubeIndex = _createdObjects.Count;
                        var cubeName = $"ReflectionCube_{cubeIndex}";
                        ReflectionHelper.SetProperty(cube, "name", cubeName);

                        // 随机位置
                        float x = (float)(_random.NextDouble() * 6 - 3); // -3 到 3
                        float y = (float)(_random.NextDouble() * 3 + 1); // 1 到 4
                        float z = (float)(_random.NextDouble() * 6 - 3); // -3 到 3

                        var transform = ReflectionHelper.GetProperty(cube, "transform");
                        if (transform != null)
                        {
                            var vector3Type = ReflectionHelper.FindType("UnityEngine.Vector3");
                            var position = Activator.CreateInstance(vector3Type, x, y, z);
                            ReflectionHelper.SetProperty(transform, "position", position);
                        }

                        // 随机颜色
                        float r = (float)_random.NextDouble();
                        float g = (float)_random.NextDouble();
                        float b = (float)_random.NextDouble();

                        var renderer = ReflectionHelper.GetComponent(cube, "UnityEngine.MeshRenderer");
                        if (renderer != null)
                        {
                            var material = ReflectionHelper.GetProperty(renderer, "material");
                            if (material != null)
                            {
                                var colorType = ReflectionHelper.FindType("UnityEngine.Color");
                                var color = Activator.CreateInstance(colorType, r, g, b, 1.0f);
                                ReflectionHelper.SetProperty(material, "color", color);
                                Logger.Log($"Set initial color for {cubeName}: RGB({r:F2}, {g:F2}, {b:F2})");
                            }
                        }

                        _createdObjects.Add(cube);
                        Logger.Log($"Created {cubeName} at position ({x:F2}, {y:F2}, {z:F2}). Total objects: {_createdObjects.Count}");
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
                var lightIndex = _createdObjects.Count;
                var lightName = $"ReflectionLight_{lightIndex}";
                var light = ReflectionHelper.CreateGameObject(lightName);

                if (light == null)
                {
                    Logger.LogError("Failed to create GameObject for light");
                    return;
                }

                var lightComponent = ReflectionHelper.AddComponent(light, "UnityEngine.Light");
                if (lightComponent != null)
                {
                    ReflectionHelper.SetProperty(lightComponent, "intensity", 1.5f);
                    ReflectionHelper.SetProperty(lightComponent, "range", 15.0f);

                    var lightType = ReflectionHelper.FindType("UnityEngine.LightType");
                    if (lightType != null)
                    {
                        var pointLight = Enum.Parse(lightType, "Point");
                        ReflectionHelper.SetProperty(lightComponent, "type", pointLight);
                    }

                    // 随机颜色
                    float r = (float)_random.NextDouble();
                    float g = (float)_random.NextDouble();
                    float b = (float)_random.NextDouble();

                    var colorType = ReflectionHelper.FindType("UnityEngine.Color");
                    var color = Activator.CreateInstance(colorType, r, g, b, 1.0f);
                    ReflectionHelper.SetProperty(lightComponent, "color", color);
                    Logger.Log($"Set light color for {lightName}: RGB({r:F2}, {g:F2}, {b:F2})");
                }

                // 随机位置
                float x = (float)(_random.NextDouble() * 8 - 4);
                float y = (float)(_random.NextDouble() * 3 + 4); // 4 到 7
                float z = (float)(_random.NextDouble() * 8 - 4);

                var transform = ReflectionHelper.GetProperty(light, "transform");
                if (transform != null)
                {
                    var vector3Type = ReflectionHelper.FindType("UnityEngine.Vector3");
                    var position = Activator.CreateInstance(vector3Type, x, y, z);
                    ReflectionHelper.SetProperty(transform, "position", position);
                }

                _createdObjects.Add(light);
                Logger.Log($"Created {lightName} at position ({x:F2}, {y:F2}, {z:F2}). Total objects: {_createdObjects.Count}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to create light: {ex.Message}");
            }
        }

        private void ChangeAllColors()
        {
            try
            {
                int changedCount = 0;
                int totalCount = _createdObjects.Count;

                for (int i = 0; i < _createdObjects.Count; i++)
                {
                    var obj = _createdObjects[i];
                    if (obj == null)
                    {
                        Logger.LogWarning($"Object at index {i} is null");
                        continue;
                    }

                    var name = ReflectionHelper.GetProperty(obj, "name") as string;
                    bool changed = false;

                    // 尝试改变网格渲染器的颜色（用于立方体等）
                    try
                    {
                        var renderer = ReflectionHelper.GetComponent(obj, "UnityEngine.MeshRenderer");
                        if (renderer != null)
                        {
                            var material = ReflectionHelper.GetProperty(renderer, "material");
                            if (material != null)
                            {
                                var colorType = ReflectionHelper.FindType("UnityEngine.Color");
                                var color = Activator.CreateInstance(colorType,
                                    (float)_random.NextDouble(),
                                    (float)_random.NextDouble(),
                                    (float)_random.NextDouble(),
                                    1.0f);
                                ReflectionHelper.SetProperty(material, "color", color);
                                changed = true;
                                Logger.Log($"Changed color for mesh: {name}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to change mesh color for {name}: {ex.Message}");
                    }

                    // 尝试改变灯光的颜色
                    try
                    {
                        var lightComponent = ReflectionHelper.GetComponent(obj, "UnityEngine.Light");
                        if (lightComponent != null)
                        {
                            var colorType = ReflectionHelper.FindType("UnityEngine.Color");
                            var color = Activator.CreateInstance(colorType,
                                (float)_random.NextDouble(),
                                (float)_random.NextDouble(),
                                (float)_random.NextDouble(),
                                1.0f);
                            ReflectionHelper.SetProperty(lightComponent, "color", color);
                            changed = true;
                            Logger.Log($"Changed color for light: {name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to change light color for {name}: {ex.Message}");
                    }

                    if (changed)
                    {
                        changedCount++;
                    }
                    else
                    {
                        Logger.LogWarning($"No color component found for: {name}");
                    }
                }

                Logger.Log($"Changed colors for {changedCount}/{totalCount} objects!");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to change colors: {ex.Message}");
            }
        }

        private void RotateAll()
        {
            try
            {
                int rotatedCount = 0;
                int totalCount = _createdObjects.Count;

                for (int i = 0; i < _createdObjects.Count; i++)
                {
                    var obj = _createdObjects[i];
                    if (obj == null)
                    {
                        Logger.LogWarning($"Object at index {i} is null");
                        continue;
                    }

                    var name = ReflectionHelper.GetProperty(obj, "name") as string;

                    try
                    {
                        var transform = ReflectionHelper.GetProperty(obj, "transform");
                        if (transform != null)
                        {
                            var vector3Type = ReflectionHelper.FindType("UnityEngine.Vector3");
                            var rotation = Activator.CreateInstance(vector3Type,
                                _random.Next(-45, 45),
                                _random.Next(-45, 45),
                                _random.Next(-45, 45));
                            ReflectionHelper.InvokeMethod(transform, "Rotate", rotation);
                            rotatedCount++;
                            Logger.Log($"Rotated: {name}");
                        }
                        else
                        {
                            Logger.LogWarning($"No transform found for: {name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to rotate {name}: {ex.Message}");
                    }
                }

                Logger.Log($"Rotated {rotatedCount}/{totalCount} objects!");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed during rotate all: {ex.Message}");
            }
        }

        private void CleanUp()
        {
            try
            {
                int count = 0;
                int totalCount = _createdObjects.Count;

                // 使用反向循环，因为我们要清除列表
                for (int i = _createdObjects.Count - 1; i >= 0; i--)
                {
                    var obj = _createdObjects[i];
                    if (obj != null)
                    {
                        var name = ReflectionHelper.GetProperty(obj, "name") as string ?? "Unknown";
                        try
                        {
                            ReflectionHelper.Destroy(obj);
                            count++;
                            Logger.Log($"Destroyed: {name}");
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"Failed to destroy {name}: {ex.Message}");
                        }
                    }
                }

                _createdObjects.Clear();
                Logger.Log($"Cleanup complete: destroyed {count}/{totalCount} objects");
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