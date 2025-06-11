// ModSystem.Unity/Debug/ModMemoryMonitor.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace ModSystem.Unity.Debug
{
    /// <summary>
    /// 模组内存监控器
    /// 监控模组的内存使用情况并提供优化建议
    /// </summary>
    [AddComponentMenu("ModSystem/Debug/Memory Monitor")]
    public class ModMemoryMonitor : MonoBehaviour
    {
        #region Configuration
        [Header("Monitor Settings")]
        [SerializeField] private bool enableMonitoring = true;
        [SerializeField] private float updateInterval = 1f;
        [SerializeField] private int historySize = 60;
        
        [Header("Display Settings")]
        [SerializeField] private bool showUI = false;
        [SerializeField] private KeyCode toggleKey = KeyCode.F9;
        [SerializeField] private Vector2 windowPosition = new Vector2(Screen.width - 410, 10);
        [SerializeField] private Vector2 windowSize = new Vector2(400, 500);
        
        [Header("Alert Settings")]
        [SerializeField] private float memoryWarningThreshold = 100f; // MB
        [SerializeField] private float memoryCriticalThreshold = 200f; // MB
        #endregion

        #region Private Fields
        private Dictionary<string, MemoryStats> modMemoryStats = new Dictionary<string, MemoryStats>();
        private float lastUpdateTime;
        private GCMemoryInfo lastGCInfo;
        private long baselineMemory;
        private Vector2 scrollPosition;
        private Tab currentTab = Tab.Overview;
        #endregion

        #region Enums
        private enum Tab
        {
            Overview,
            Details,
            History,
            Alerts
        }
        #endregion

        #region Data Structures
        /// <summary>
        /// 内存统计信息
        /// </summary>
        public class MemoryStats
        {
            public string ModId { get; set; }
            public long TotalAllocated { get; set; }
            public long CurrentUsage { get; set; }
            public int ObjectCount { get; set; }
            public int TextureMemory { get; set; }
            public int MeshMemory { get; set; }
            public int AudioMemory { get; set; }
            public int MaterialCount { get; set; }
            public List<float> UsageHistory { get; set; } = new List<float>();
            public DateTime LastUpdate { get; set; }
            public List<MemoryAlert> Alerts { get; set; } = new List<MemoryAlert>();
            
            // 详细的对象统计
            public Dictionary<string, int> ObjectTypeCount { get; set; } = new Dictionary<string, int>();
            public List<LargeObjectInfo> LargeObjects { get; set; } = new List<LargeObjectInfo>();
        }
        
        /// <summary>
        /// 大对象信息
        /// </summary>
        public class LargeObjectInfo
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public long Size { get; set; }
            public UnityEngine.Object Reference { get; set; }
        }
        
        /// <summary>
        /// 内存警报
        /// </summary>
        public class MemoryAlert
        {
            public DateTime Time { get; set; }
            public AlertType Type { get; set; }
            public string Message { get; set; }
            public float MemoryUsage { get; set; }
        }
        
        /// <summary>
        /// 警报类型
        /// </summary>
        public enum AlertType
        {
            Info,
            Warning,
            Critical
        }
        #endregion
    }
} 