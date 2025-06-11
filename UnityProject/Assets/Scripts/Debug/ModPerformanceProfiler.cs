// ModSystem.Unity/Debug/ModPerformanceProfiler.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace ModSystem.Unity.Debug
{
    /// <summary>
    /// 模组性能分析器
    /// 提供详细的性能分析和优化建议
    /// </summary>
    [AddComponentMenu("ModSystem/Debug/Performance Profiler")]
    public class ModPerformanceProfiler : MonoBehaviour
    {
        #region Singleton
        private static ModPerformanceProfiler instance;
        
        /// <summary>
        /// 获取性能分析器实例
        /// </summary>
        public static ModPerformanceProfiler Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("ModPerformanceProfiler");
                    instance = go.AddComponent<ModPerformanceProfiler>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }
        #endregion

        #region Configuration
        [Header("Profiler Settings")]
        [SerializeField] private bool enableProfiling = true;
        [SerializeField] private bool showUI = false;
        [SerializeField] private KeyCode toggleKey = KeyCode.F10;
        [SerializeField] private int maxSampleHistory = 100;
        
        [Header("Display Settings")]
        [SerializeField] private Vector2 windowPosition = new Vector2(10, 10);
        [SerializeField] private Vector2 windowSize = new Vector2(800, 600);
        #endregion

        #region Private Fields
        private Dictionary<string, ProfileData> profileData = new Dictionary<string, ProfileData>();
        private Dictionary<string, Stopwatch> activeTimers = new Dictionary<string, Stopwatch>();
        private Vector2 scrollPosition;
        private SortMode sortMode = SortMode.TotalTime;
        private string filterText = "";
        private Tab currentTab = Tab.Overview;
        private readonly object lockObject = new object();
        #endregion

        #region Enums
        private enum SortMode
        {
            Name,
            CallCount,
            TotalTime,
            AverageTime,
            LastTime,
            MaxTime
        }
        
        private enum Tab
        {
            Overview,
            Details,
            Timeline,
            Recommendations
        }
        #endregion

        #region Data Structures
        /// <summary>
        /// 性能数据
        /// </summary>
        public class ProfileData
        {
            public string Name { get; set; }
            public int CallCount { get; set; }
            public double TotalTime { get; set; }
            public double MinTime { get; set; } = double.MaxValue;
            public double MaxTime { get; set; } = double.MinValue;
            public double AverageTime => CallCount > 0 ? TotalTime / CallCount : 0;
            public double LastTime { get; set; }
            public Queue<double> RecentTimes { get; set; } = new Queue<double>();
            public long AllocatedMemory { get; set; }
            public int GCCount { get; set; }
            public Stack<Stopwatch> TimerStack { get; set; } = new Stack<Stopwatch>();
        }
        
        /// <summary>
        /// 性能采样范围
        /// </summary>
        public class ProfileScope : IDisposable
        {
            private readonly ModPerformanceProfiler profiler;
            private readonly string name;
            private readonly long startMemory;
            
            public ProfileScope(ModPerformanceProfiler profiler, string name)
            {
                this.profiler = profiler;
                this.name = name;
                this.startMemory = GC.GetTotalMemory(false);
                profiler.BeginSample(name);
            }
            
            public void Dispose()
            {
                var endMemory = GC.GetTotalMemory(false);
                var allocatedMemory = endMemory - startMemory;
                profiler.EndSample(name, allocatedMemory);
            }
        }
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                showUI = !showUI;
            }
            
            // 定期清理超时的计时器
            if (Time.frameCount % 300 == 0) // 每300帧清理一次
            {
                CleanupStaleTimers();
            }
        }
        
        void OnGUI()
        {
            if (!showUI) return;
            
            var rect = new Rect(windowPosition, windowSize);
            GUI.Window(1, rect, DrawProfilerWindow, "Mod Performance Profiler");
        }
        #endregion

        #region Profiling Methods
        /// <summary>
        /// 开始采样
        /// </summary>
        public void BeginSample(string name)
        {
            if (!enableProfiling) return;
            
            lock (lockObject)
            {
                if (!profileData.ContainsKey(name))
                {
                    profileData[name] = new ProfileData { Name = name };
                }
                
                var sw = Stopwatch.StartNew();
                profileData[name].TimerStack.Push(sw);
                
                // Unity Profiler集成
                Profiler.BeginSample($"Mod_{name}");
            }
        }
        
        /// <summary>
        /// 结束采样
        /// </summary>
        public void EndSample(string name, long allocatedMemory = 0)
        {
            if (!enableProfiling) return;
            
            Profiler.EndSample();
            
            lock (lockObject)
            {
                if (profileData.TryGetValue(name, out var data) && data.TimerStack.Count > 0)
                {
                    var sw = data.TimerStack.Pop();
                    sw.Stop();
                    
                    var elapsed = sw.Elapsed.TotalMilliseconds;
                    
                    // 更新统计数据
                    data.CallCount++;
                    data.TotalTime += elapsed;
                    data.LastTime = elapsed;
                    data.MinTime = Math.Min(data.MinTime, elapsed);
                    data.MaxTime = Math.Max(data.MaxTime, elapsed);
                    
                    // 记录内存分配
                    if (allocatedMemory > 0)
                    {
                        data.AllocatedMemory += allocatedMemory;
                    }
                    
                    // 保留最近的采样时间
                    data.RecentTimes.Enqueue(elapsed);
                    if (data.RecentTimes.Count > maxSampleHistory)
                        data.RecentTimes.Dequeue();
                }
            }
        }
        
        /// <summary>
        /// 创建性能采样范围
        /// </summary>
        public ProfileScope BeginScope(string name)
        {
            return new ProfileScope(this, name);
        }
        
        /// <summary>
        /// 标记事件
        /// </summary>
        public void MarkEvent(string eventName)
        {
            if (!enableProfiling) return;
            
            UnityEngine.Debug.Log($"[Performance Event] {eventName} at {Time.time:F3}s");
        }
        #endregion
    }
} 