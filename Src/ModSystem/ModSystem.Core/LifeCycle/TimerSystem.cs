using System;
using System.Collections.Generic;

namespace ModSystem.Core.Lifecycle
{
    /// <summary>
    /// 简单的定时器系统
    /// </summary>
    public class TimerSystem
    {
        private readonly List<Timer> _timers = new List<Timer>();
        private readonly List<Timer> _timersToRemove = new List<Timer>();
        private int _nextTimerId = 1;

        /// <summary>
        /// 设置一次性定时器
        /// </summary>
        /// <param name="delay">延迟时间（秒）</param>
        /// <param name="callback">回调函数</param>
        /// <returns>定时器ID</returns>
        public int SetTimer(float delay, Action callback)
        {
            if (callback == null || delay < 0) return -1;

            var timer = new Timer
            {
                Id = _nextTimerId++,
                Delay = delay,
                ElapsedTime = 0,
                Callback = callback,
                IsRepeating = false
            };

            _timers.Add(timer);
            return timer.Id;
        }

        /// <summary>
        /// 设置重复定时器
        /// </summary>
        /// <param name="interval">间隔时间（秒）</param>
        /// <param name="callback">回调函数</param>
        /// <returns>定时器ID</returns>
        public int SetRepeatingTimer(float interval, Action callback)
        {
            if (callback == null || interval <= 0) return -1;

            var timer = new Timer
            {
                Id = _nextTimerId++,
                Delay = interval,
                ElapsedTime = 0,
                Callback = callback,
                IsRepeating = true
            };

            _timers.Add(timer);
            return timer.Id;
        }

        /// <summary>
        /// 取消定时器
        /// </summary>
        /// <param name="timerId">定时器ID</param>
        public void CancelTimer(int timerId)
        {
            _timers.RemoveAll(t => t.Id == timerId);
        }

        /// <summary>
        /// 更新所有定时器
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        public void Update(float deltaTime)
        {
            _timersToRemove.Clear();

            // 更新所有定时器
            foreach (var timer in _timers)
            {
                timer.ElapsedTime += deltaTime;

                if (timer.ElapsedTime >= timer.Delay)
                {
                    // 触发回调
                    try
                    {
                        timer.Callback?.Invoke();
                    }
                    catch
                    {
                        // 静默处理异常
                    }

                    if (timer.IsRepeating)
                    {
                        // 重置重复定时器
                        timer.ElapsedTime -= timer.Delay;
                    }
                    else
                    {
                        // 标记一次性定时器待删除
                        _timersToRemove.Add(timer);
                    }
                }
            }

            // 移除已完成的一次性定时器
            foreach (var timer in _timersToRemove)
            {
                _timers.Remove(timer);
            }
        }

        /// <summary>
        /// 清除所有定时器
        /// </summary>
        public void Clear()
        {
            _timers.Clear();
            _timersToRemove.Clear();
        }

        /// <summary>
        /// 获取活动定时器数量
        /// </summary>
        public int GetActiveTimerCount() => _timers.Count;

        /// <summary>
        /// 定时器数据结构
        /// </summary>
        private class Timer
        {
            public int Id { get; set; }
            public float Delay { get; set; }
            public float ElapsedTime { get; set; }
            public Action Callback { get; set; }
            public bool IsRepeating { get; set; }
        }
    }
}