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

            // 创建文本显示（如果可能）
            try
            {
                // 创建Canvas（如果不存在）
                var canvas = ReflectionHelper.FindGameObject("ModUICanvas");
                if (canvas == null)
                {
                    canvas = UnityHelper.CreateCanvas("ModUICanvas");
                }

                // 创建文本
                _timerText = UnityHelper.CreateText(canvas, "Timer: 0.0s | Ticks: 0", 0, 100);

                // 设置文本属性
                var textComponent = ReflectionHelper.GetComponent(_timerText, "UnityEngine.UI.Text");
                if (textComponent != null)
                {
                    ReflectionHelper.SetProperty(textComponent, "fontSize", 24);
                    ReflectionHelper.SetProperty(textComponent, "alignment", 4); // MiddleCenter
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
            Logger.Log("Setting one-time timer for 3 seconds...");

            SetTimer(3.0f, () =>
            {
                Logger.Log("One-time timer fired after 3 seconds!");

                // 创建一个临时对象并在1秒后删除
                var tempSphere = UnityHelper.CreateSphere("TempSphere");
                UnityHelper.SetPosition(tempSphere, 3, 2, 0);
                UnityHelper.SetColor(tempSphere, 1, 1, 0);

                SetTimer(1.0f, () =>
                {
                    ReflectionHelper.Destroy(tempSphere);
                    Logger.Log("Temporary sphere destroyed");
                });
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
                    var textComponent = ReflectionHelper.GetComponent(_timerText, "UnityEngine.UI.Text");
                    if (textComponent != null)
                    {
                        string displayText = $"Timer: {_totalTime:F1}s | Ticks: {_tickCount}";
                        ReflectionHelper.SetProperty(textComponent, "text", displayText);
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
        }

        protected override void OnShutdown()
        {
            PauseTimers();
            CleanupObjects();
            Logger.Log("TimerMod shutdown");
        }
    }
}