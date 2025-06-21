using System;
using System.Collections.Generic;
using ModSystem.Core.Runtime;
using ModSystem.Core.Events;
using ModSystem.Core.Unity;
using ModSystem.Core.Reflection;

namespace ConfigurableMod
{
    /// <summary>
    /// 可配置模组 - 演示V5配置系统
    /// </summary>
    public class ConfigurableModExample : ModBase
    {
        public override string ModId => "configurable_mod";

        // 配置对象
        private ModConfig _config;

        // 创建的对象
        private object _configurableObject;
        private float _rotationSpeed;

        protected override void OnInitialize()
        {
            // V5核心功能：加载配置
            _config = LoadConfig<ModConfig>();

            Logger.Log($"ConfigurableMod initialized with config:");
            Logger.Log($"  - Object Name: {_config.ObjectName}");
            Logger.Log($"  - Color: R={_config.Color.R}, G={_config.Color.G}, B={_config.Color.B}");
            Logger.Log($"  - Size: {_config.Size}");
            Logger.Log($"  - Auto Rotate: {_config.AutoRotate}");
            Logger.Log($"  - Rotation Speed: {_config.RotationSpeed}");

            // 订阅按钮事件
            Subscribe<ButtonClickedEvent>(OnButtonClicked);

            // 创建UI
            PublishEvent(new CreateUIRequestEvent
            {
                UIType = "ConfigurablePanel",
                Title = "Configurable Mod",
                Buttons = new[]
                {
                    new ButtonConfig { Id = "create_object", Text = "Create Object" },
                    new ButtonConfig { Id = "reload_config", Text = "Reload Config" },
                    new ButtonConfig { Id = "change_size", Text = "Change Size" },
                    new ButtonConfig { Id = "save_config", Text = "Save Config" },
                    new ButtonConfig { Id = "cleanup", Text = "Clean Up" }
                }
            });
        }

        private void OnButtonClicked(ButtonClickedEvent e)
        {
            switch (e.ButtonId)
            {
                case "create_object":
                    CreateConfiguredObject();
                    break;
                case "reload_config":
                    ReloadConfiguration();
                    break;
                case "change_size":
                    ChangeSizeRandomly();
                    break;
                case "save_config":
                    SaveCurrentConfig();
                    break;
                case "cleanup":
                    CleanUp();
                    break;
            }
        }

        /// <summary>
        /// 创建配置的对象
        /// </summary>
        private void CreateConfiguredObject()
        {
            try
            {
                // 清理旧对象
                if (_configurableObject != null)
                {
                    CleanUp();
                }

                // 根据配置创建对象
                switch (_config.ObjectType.ToLower())
                {
                    case "cube":
                        _configurableObject = UnityHelper.CreateCube(_config.ObjectName);
                        break;
                    case "sphere":
                        _configurableObject = UnityHelper.CreateSphere(_config.ObjectName);
                        break;
                    case "plane":
                        _configurableObject = UnityHelper.CreatePlane(_config.ObjectName);
                        break;
                    default:
                        _configurableObject = UnityHelper.CreateCube(_config.ObjectName);
                        Logger.LogWarning($"Unknown object type '{_config.ObjectType}', using cube");
                        break;
                }

                // 应用配置
                ApplyConfiguration();

                Logger.Log($"Created {_config.ObjectType} with configured properties");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to create object: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用配置到对象
        /// </summary>
        private void ApplyConfiguration()
        {
            if (_configurableObject == null) return;

            // 设置位置
            UnityHelper.SetPosition(_configurableObject,
                _config.Position.X,
                _config.Position.Y,
                _config.Position.Z);

            // 设置缩放
            UnityHelper.SetScale(_configurableObject,
                _config.Size,
                _config.Size,
                _config.Size);

            // 设置颜色
            UnityHelper.SetColor(_configurableObject,
                _config.Color.R,
                _config.Color.G,
                _config.Color.B);

            // 设置旋转速度
            _rotationSpeed = _config.AutoRotate ? _config.RotationSpeed : 0f;
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        private void ReloadConfiguration()
        {
            Logger.Log("Reloading configuration...");

            // 重新加载配置
            _config = ReloadConfig<ModConfig>();

            // 如果有对象，应用新配置
            if (_configurableObject != null)
            {
                ApplyConfiguration();
                Logger.Log("Configuration reloaded and applied!");
            }
            else
            {
                Logger.Log("Configuration reloaded (no object to apply to)");
            }
        }

        /// <summary>
        /// 随机改变大小（演示运行时修改配置）
        /// </summary>
        private void ChangeSizeRandomly()
        {
            if (_configurableObject == null)
            {
                Logger.LogWarning("No object created yet!");
                return;
            }

            // 随机新大小
            var random = new Random();
            _config.Size = (float)(random.NextDouble() * 2 + 0.5); // 0.5 到 2.5

            // 应用到对象
            UnityHelper.SetScale(_configurableObject,
                _config.Size,
                _config.Size,
                _config.Size);

            Logger.Log($"Changed size to: {_config.Size:F2}");
        }

        /// <summary>
        /// 保存当前配置
        /// </summary>
        private void SaveCurrentConfig()
        {
            if (SaveConfig(_config))
            {
                Logger.Log("Configuration saved successfully!");
            }
            else
            {
                Logger.LogError("Failed to save configuration");
            }
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        public override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);

            // 自动旋转
            if (_configurableObject != null && _rotationSpeed > 0)
            {
                UnityHelper.Rotate(_configurableObject, 0, _rotationSpeed * deltaTime, 0);
            }
        }

        /// <summary>
        /// 清理
        /// </summary>
        private void CleanUp()
        {
            if (_configurableObject != null)
            {
                try
                {
                    ReflectionHelper.Destroy(_configurableObject);
                    _configurableObject = null;
                    Logger.Log("Cleaned up object");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to clean up: {ex.Message}");
                }
            }
        }

        protected override void OnShutdown()
        {
            CleanUp();
        }
    }

    /// <summary>
    /// 模组配置类
    /// </summary>
    public class ModConfig
    {
        // 对象基本属性
        public string ObjectName { get; set; } = "ConfiguredObject";
        public string ObjectType { get; set; } = "Cube";

        // 外观
        public ColorConfig Color { get; set; } = new ColorConfig { R = 1.0f, G = 0.5f, B = 0.0f };
        public float Size { get; set; } = 1.5f;

        // 位置
        public PositionConfig Position { get; set; } = new PositionConfig { X = 0, Y = 2, Z = 0 };

        // 行为
        public bool AutoRotate { get; set; } = true;
        public float RotationSpeed { get; set; } = 30.0f;

        // 高级选项
        public bool EnableEffects { get; set; } = false;
        public List<string> Tags { get; set; } = new List<string> { "configurable", "example" };
    }

    /// <summary>
    /// 颜色配置
    /// </summary>
    public class ColorConfig
    {
        public float R { get; set; }
        public float G { get; set; }
        public float B { get; set; }
    }

    /// <summary>
    /// 位置配置
    /// </summary>
    public class PositionConfig
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
}