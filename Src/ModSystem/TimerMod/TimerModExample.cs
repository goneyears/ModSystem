using System;
using System.Collections.Generic;
using ModSystem.Core.Runtime;
using ModSystem.Core.Events;
using ModSystem.Core.Unity;
using ModSystem.Core.Reflection;

namespace TimerMod
{
    /// <summary>
    /// 定时器示例模组 - 演示V4版本的生命周期和定时器功能
    /// </summary>
    public class TimerModExample : ModBase
    {
        public override string ModId => "timer_mod";

        // 状态变量
        private float _totalTime = 0f;
        private int _tickCount = 0;
        private int _colorChangeTimer = -1;
        private int _broadcastTimer = -1;

        // 创建的对象
        private object _timerDisplay;
        private object _rotatingCube;
        private object _timerText;

        // 帧计数器（替代Unity的Time.frameCount）
        private int _frameCount = 0;

        // 防止重复触发一次性定时器
        private bool _isWaitingForOnceTimer = false;

        // 颜色列表
        private readonly float[][] _colors = new float[][]
        {
            new float[] { 1, 0, 0 },    // 红
            new float[] { 0, 1, 0 },    // 绿
            new float[] { 0, 0, 1 },    // 蓝
            new float[] { 1, 1, 0 },    // 黄
            new float[] { 1, 0, 1 },    // 品红
            new float[] { 0, 1, 1 }     // 青
        };
        private int _currentColorIndex = 0;

        protected override void OnInitialize()
        {
            Logger.Log("TimerMod initialized! Demonstrating lifecycle and timers.");

            // 订阅按钮事件
            Subscribe<ButtonClickedEvent>(OnButtonClicked);

            // 创建UI
            PublishEvent(new CreateUIRequestEvent
            {
                UIType = "TimerPanel",
                Title = "Timer Mod Demo",
                Buttons = new[]
                {
                    new ButtonConfig { Id = "start_demo", Text = "Start Demo" },
                    new ButtonConfig { Id = "pause_timers", Text = "Pause Timers" },
                    new ButtonConfig { Id = "reset", Text = "Reset" },
                    new ButtonConfig { Id = "test_once", Text = "Test Once (3s)" }
                }
            });
        }

        private void OnButtonClicked(ButtonClickedEvent e)
        {
            switch (e.ButtonId)
            {
                case "start_demo":
                    StartDemo();
                    break;
                case "pause_timers":
                    PauseTimers();
                    break;
                case "reset":
                    Reset();
                    break;
                case "test_once":
                    TestOnceTimer();
                    break;
            }
        }

        /// <summary>
        /// 开始演示
        /// </summary>
        private void StartDemo()
        {
            try
            {
                // 创建显示对象
                CreateDisplayObjects();

                // 设置重复定时器：每秒改变颜色
                _colorChangeTimer = SetRepeatingTimer(1.0f, () =>
                {
                    ChangeColor();
                    _tickCount++;
                    UpdateDisplay();
                });
                Logger.Log("Started color change timer (1s interval)");

                // 设置重复定时器：每5秒广播消息
                _broadcastTimer = SetRepeatingTimer(5.0f, () =>
                {
                    PublishEvent(new BroadcastEvent
                    {
                        Message = $"Timer broadcast: {_tickCount} ticks, {_totalTime:F1}s elapsed",
                        Data = new Dictionary<string, object>
                        {
                            { "ticks", _tickCount },
                            { "time", _totalTime }
                        }
                    });
                    Logger.Log("Broadcast sent!");
                });
                Logger.Log("Started broadcast timer (5s interval)");

                Logger.Log("Demo started! Watch the cube rotate and change color.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to start demo: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建显示对象
        /// </summary>
        private void CreateDisplayObjects()
        {
            // 清理旧对象
            CleanupObjects();

            // 创建旋转的立方体
            _rotatingCube = UnityHelper.CreateCube("TimerCube");
            UnityHelper.SetPosition(_rotatingCube, 0, 2, 0);
            UnityHelper.SetScale(_rotatingCube, 1.5f, 1.5f, 1.5f);
            UnityHelper.SetColor(_rotatingCube, 1, 0, 0);

            // 创建3D文本显示（使用3D Text而不是UI Text）
            try
            {
                // 创建一个空的GameObject作为文本载体
                _timerText = ReflectionHelper.CreateGameObject("TimerDisplay");
                UnityHelper.SetPosition(_timerText, 0, 4, 0);

                // 添加TextMesh组件（3D文本）
                var textMeshType = ReflectionHelper.FindType("UnityEngine.TextMesh");
                if (textMeshType != null)
                {
                    var textMesh = ReflectionHelper.AddComponent(_timerText, textMeshType);
                    if (textMesh != null)
                    {
                        ReflectionHelper.SetProperty(textMesh, "text", "Timer: 0.0s | Ticks: 0");
                        ReflectionHelper.SetProperty(textMesh, "fontSize", 50);
                        ReflectionHelper.SetProperty(textMesh, "characterSize", 0.1f);
                        ReflectionHelper.SetProperty(textMesh, "anchor", 4); // MiddleCenter
                        ReflectionHelper.SetProperty(textMesh, "alignment", 1); // Center

                        // 设置颜色
                        var colorType = ReflectionHelper.FindType("UnityEngine.Color");
                        var whiteColor = Activator.CreateInstance(colorType, 1f, 1f, 1f, 1f);
                        ReflectionHelper.SetProperty(textMesh, "color", whiteColor);

                        Logger.Log("Created 3D text display");
                    }
                }
                else
                {
                    Logger.LogWarning("TextMesh type not found");
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not create text display: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试一次性定时器
        /// </summary>
        private void TestOnceTimer()
        {
            if (_isWaitingForOnceTimer)
            {
                Logger.LogWarning("Already waiting for timer, please wait...");
                return;
            }

            _isWaitingForOnceTimer = true;
            Logger.Log("Setting one-time timer for 3 seconds...");

            // 创建一个唯一的球体名称，避免重复
            var sphereName = $"TempSphere_{DateTime.Now.Ticks}";

            SetTimer(3.0f, () =>
            {
                try
                {
                    Logger.Log("One-time timer fired after 3 seconds!");

                    // 创建一个临时对象
                    var tempSphere = UnityHelper.CreateSphere(sphereName);
                    if (tempSphere != null)
                    {
                        UnityHelper.SetPosition(tempSphere, 3, 2, 0);
                        UnityHelper.SetColor(tempSphere, 1, 1, 0);
                        Logger.Log($"Created sphere: {sphereName}");

                        // 1秒后删除（使用闭包确保引用正确）
                        var sphereToDelete = tempSphere;
                        var nameToLog = sphereName;
                        SetTimer(1.0f, () =>
                        {
                            try
                            {
                                if (sphereToDelete != null)
                                {
                                    ReflectionHelper.Destroy(sphereToDelete);
                                    Logger.Log($"Temporary sphere {nameToLog} destroyed");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError($"Failed to destroy sphere: {ex.Message}");
                            }
                        });
                    }
                    else
                    {
                        Logger.LogError("Failed to create temporary sphere");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error in timer callback: {ex.Message}");
                }
                finally
                {
                    // 确保重置标志
                    _isWaitingForOnceTimer = false;
                }
            });
        }

        /// <summary>
        /// 暂停定时器
        /// </summary>
        private void PauseTimers()
        {
            if (_colorChangeTimer != -1)
            {
                CancelTimer(_colorChangeTimer);
                _colorChangeTimer = -1;
                Logger.Log("Color change timer paused");
            }

            if (_broadcastTimer != -1)
            {
                CancelTimer(_broadcastTimer);
                _broadcastTimer = -1;
                Logger.Log("Broadcast timer paused");
            }
        }

        /// <summary>
        /// 重置演示
        /// </summary>
        private void Reset()
        {
            PauseTimers();
            CleanupObjects();

            _totalTime = 0f;
            _tickCount = 0;
            _currentColorIndex = 0;
            _frameCount = 0;
            _isWaitingForOnceTimer = false;

            Logger.Log("Demo reset");
        }

        /// <summary>
        /// 每帧更新（重写基类方法）
        /// </summary>
        public override void OnUpdate(float deltaTime)
        {
            // 调用基类的Update（更新定时器）
            base.OnUpdate(deltaTime);

            // 累计时间
            _totalTime += deltaTime;

            // 增加帧计数
            _frameCount++;

            // 旋转立方体
            if (_rotatingCube != null)
            {
                float rotationSpeed = 30f; // 每秒30度
                UnityHelper.Rotate(_rotatingCube, 0, rotationSpeed * deltaTime, 0);
            }

            // 更新显示（每10帧更新一次，避免频繁更新）
            if (_frameCount % 10 == 0)
            {
                UpdateDisplay();
            }
        }

        /// <summary>
        /// 固定更新（可用于物理）
        /// </summary>
        public override void OnFixedUpdate(float fixedDeltaTime)
        {
            // 这里可以添加物理相关的更新
        }

        /// <summary>
        /// 改变颜色
        /// </summary>
        private void ChangeColor()
        {
            if (_rotatingCube != null)
            {
                var color = _colors[_currentColorIndex];
                UnityHelper.SetColor(_rotatingCube, color[0], color[1], color[2]);

                _currentColorIndex = (_currentColorIndex + 1) % _colors.Length;
                Logger.Log($"Changed color to index {_currentColorIndex}");
            }
        }

        /// <summary>
        /// 更新显示
        /// </summary>
        private void UpdateDisplay()
        {
            if (_timerText != null)
            {
                try
                {
                    // 更新3D文本
                    var textMesh = ReflectionHelper.GetComponent(_timerText, "UnityEngine.TextMesh");
                    if (textMesh != null)
                    {
                        string displayText = $"Timer: {_totalTime:F1}s | Ticks: {_tickCount}";
                        ReflectionHelper.SetProperty(textMesh, "text", displayText);
                    }
                }
                catch
                {
                    // 静默处理更新失败
                }
            }
        }

        /// <summary>
        /// 清理对象
        /// </summary>
        private void CleanupObjects()
        {
            if (_rotatingCube != null)
            {
                ReflectionHelper.Destroy(_rotatingCube);
                _rotatingCube = null;
            }

            if (_timerText != null)
            {
                ReflectionHelper.Destroy(_timerText);
                _timerText = null;
            }

            // 清理可能存在的对象
            CleanupGameObject("TimerCube");
            CleanupGameObject("TimerDisplay");

            // 清理所有临时球体（通过名称前缀查找）
            CleanupTempSpheres();
        }

        /// <summary>
        /// 清理指定名称的游戏对象
        /// </summary>
        private void CleanupGameObject(string name)
        {
            try
            {
                var obj = ReflectionHelper.FindGameObject(name);
                if (obj != null)
                {
                    ReflectionHelper.Destroy(obj);
                    Logger.Log($"Cleaned up: {name}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to cleanup {name}: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理所有临时球体
        /// </summary>
        private void CleanupTempSpheres()
        {
            try
            {
                // 注意：这个方法在实际Unity中需要更复杂的实现
                // 这里只是示例，实际需要遍历场景中的所有对象
                Logger.Log("Cleaning up temporary spheres...");

                // 尝试清理一些可能的球体（有限的尝试）
                for (int i = 0; i < 10; i++)
                {
                    var sphereVariants = new[] { "TempSphere", "Sphere" };
                    foreach (var variant in sphereVariants)
                    {
                        var obj = ReflectionHelper.FindGameObject(variant);
                        if (obj != null)
                        {
                            var name = ReflectionHelper.GetProperty(obj, "name") as string;
                            if (name != null && name.StartsWith("TempSphere_"))
                            {
                                ReflectionHelper.Destroy(obj);
                                Logger.Log($"Cleaned up temporary sphere: {name}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error cleaning temporary spheres: {ex.Message}");
            }
        }

        protected override void OnShutdown()
        {
            PauseTimers();
            CleanupObjects();
            Logger.Log("TimerMod shutdown");
        }
    }
}