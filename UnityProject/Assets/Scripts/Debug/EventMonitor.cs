// ModSystem.Unity/Debug/EventMonitor.cs
using UnityEngine;
using ModSystem.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ModSystem.Unity.Debug
{
    /// <summary>
    /// 事件监视器
    /// 用于监控和调试系统中的事件流
    /// </summary>
    [AddComponentMenu("ModSystem/Debug/Event Monitor")]
    public class EventMonitor : MonoBehaviour
    {
        #region Inspector Fields
        [SerializeField] private bool showUI = true;
        [SerializeField] private bool logEventsToConsole = false;
        [SerializeField] private int maxEventHistory = 100;
        [SerializeField] private bool saveLogsToFile = false;
        #endregion

        #region Private Fields
        private IEventBus eventBus;
        private List<EventLogEntry> eventHistory = new List<EventLogEntry>();
        private Dictionary<string, int> eventCounts = new Dictionary<string, int>();
        private HashSet<string> mutedEventTypes = new HashSet<string>();
        private Vector2 scrollPosition;
        private Rect windowRect = new Rect(10, 10, 600, 400);
        private string filterText = "";
        private bool showOnlyErrors = false;
        private bool isPaused = false;
        private bool isWindowMinimized = false;
        private Tab currentTab = Tab.Events;
        #endregion

        #region Enums
        private enum Tab
        {
            Events,
            Statistics,
            Filters,
            Settings
        }
        #endregion

        #region Unity Lifecycle
        void Start()
        {
            // 获取事件总线
            var controller = ModSystemController.Instance;
            if (controller != null)
            {
                eventBus = controller.EventBus;
                
                // 订阅所有事件
                SubscribeToAllEvents();
            }
        }
        
        void OnDestroy()
        {
            // 取消订阅
            if (eventBus != null)
            {
                UnsubscribeFromAllEvents();
            }
        }
        
        void OnGUI()
        {
            if (showUI)
            {
                windowRect = GUILayout.Window(0, windowRect, DrawWindow, "Event Monitor");
            }
        }
        #endregion

        #region Event Handling
        /// <summary>
        /// 订阅所有事件
        /// </summary>
        private void SubscribeToAllEvents()
        {
            if (eventBus != null)
            {
                // 通用事件处理程序
                eventBus.SubscribeToAll(OnEvent);
                
                UnityEngine.Debug.Log("EventMonitor: Subscribed to all events");
            }
        }
        
        /// <summary>
        /// 取消订阅所有事件
        /// </summary>
        private void UnsubscribeFromAllEvents()
        {
            if (eventBus != null)
            {
                eventBus.UnsubscribeAll(this);
                UnityEngine.Debug.Log("EventMonitor: Unsubscribed from all events");
            }
        }
        
        /// <summary>
        /// 处理所有事件
        /// </summary>
        private void OnEvent(IModEvent eventData)
        {
            if (isPaused || eventData == null)
                return;
            
            // 检查是否是静音的事件类型
            if (mutedEventTypes.Contains(eventData.EventId))
                return;
            
            // 记录事件
            var logEntry = new EventLogEntry
            {
                EventType = eventData.EventId,
                SenderId = eventData.SenderId,
                Timestamp = DateTime.Now,
                Message = FormatEventMessage(eventData),
                IsError = eventData is ModErrorEvent,
                Color = GetEventColor(eventData)
            };
            
            // 添加到历史记录
            eventHistory.Add(logEntry);
            
            // 限制历史记录大小
            if (eventHistory.Count > maxEventHistory)
            {
                eventHistory.RemoveAt(0);
            }
            
            // 更新统计信息
            if (!eventCounts.ContainsKey(eventData.EventId))
            {
                eventCounts[eventData.EventId] = 0;
            }
            eventCounts[eventData.EventId]++;
            
            // 可选: 记录到控制台
            if (logEventsToConsole)
            {
                if (eventData is ModErrorEvent errorEvent)
                {
                    UnityEngine.Debug.LogError($"Event: {errorEvent.EventId} - {errorEvent.Message}\n{errorEvent.StackTrace}");
                }
                else
                {
                    UnityEngine.Debug.Log($"Event: {eventData.EventId} from {eventData.SenderId}");
                }
            }
            
            // 可选: 记录到文件
            if (saveLogsToFile)
            {
                // TODO: 实现日志文件记录
            }
        }
        #endregion

        #region UI Drawing
        /// <summary>
        /// 绘制窗口
        /// </summary>
        private void DrawWindow(int windowID)
        {
            GUILayout.BeginVertical();
            
            if (!isWindowMinimized)
            {
                // 标题栏
                DrawTitleBar();
                
                // 工具栏
                DrawToolbar();
                
                // 选项卡
                DrawTabs();
                
                // 内容区域
                DrawContent();
            }
            
            GUILayout.EndVertical();
            
            GUI.DragWindow();
        }
        
        /// <summary>
        /// 绘制标题栏
        /// </summary>
        private void DrawTitleBar()
        {
            GUILayout.BeginHorizontal();
            
            GUILayout.Label($"Events: {eventHistory.Count} | " +
                          $"Types: {eventCounts.Count} | " +
                          $"{(isPaused ? "PAUSED" : "LIVE")}", 
                          GetStatusStyle());
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button(isWindowMinimized ? "□" : "—", GUILayout.Width(20)))
            {
                isWindowMinimized = !isWindowMinimized;
            }
            
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                showUI = false;
            }
            
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制工具栏
        /// </summary>
        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal("box");
            
            // 暂停/恢复按钮
            if (GUILayout.Button(isPaused ? "▶ Resume" : "‖ Pause", GUILayout.Width(80)))
            {
                isPaused = !isPaused;
            }
            
            // 清除按钮
            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                ClearHistory();
            }
            
            // 导出按钮
            if (GUILayout.Button("Export", GUILayout.Width(60)))
            {
                ExportLog();
            }
            
            GUILayout.Space(20);
            
            // 过滤器
            GUILayout.Label("Filter:", GUILayout.Width(50));
            filterText = GUILayout.TextField(filterText, GUILayout.Width(150));
            
            // 错误过滤
            showOnlyErrors = GUILayout.Toggle(showOnlyErrors, "Errors Only");
            
            GUILayout.FlexibleSpace();
            
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制选项卡
        /// </summary>
        private void DrawTabs()
        {
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Toggle(currentTab == Tab.Events, "Events", "Button"))
                currentTab = Tab.Events;
            
            if (GUILayout.Toggle(currentTab == Tab.Statistics, "Statistics", "Button"))
                currentTab = Tab.Statistics;
            
            if (GUILayout.Toggle(currentTab == Tab.Filters, "Filters", "Button"))
                currentTab = Tab.Filters;
            
            if (GUILayout.Toggle(currentTab == Tab.Settings, "Settings", "Button"))
                currentTab = Tab.Settings;
            
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制内容
        /// </summary>
        private void DrawContent()
        {
            switch (currentTab)
            {
                case Tab.Events:
                    DrawEventList();
                    break;
                case Tab.Statistics:
                    DrawStatistics();
                    break;
                case Tab.Filters:
                    DrawFilters();
                    break;
                case Tab.Settings:
                    DrawSettings();
                    break;
            }
        }
        
        /// <summary>
        /// 绘制事件列表
        /// </summary>
        private void DrawEventList()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            
            var filteredEvents = GetFilteredEvents();
            
            foreach (var entry in filteredEvents)
            {
                DrawEventEntry(entry);
            }
            
            GUILayout.EndScrollView();
        }
        
        /// <summary>
        /// 绘制单个事件条目
        /// </summary>
        private void DrawEventEntry(EventLogEntry entry)
        {
            GUILayout.BeginHorizontal("box");
            
            // 时间戳
            GUI.color = Color.gray;
            GUILayout.Label(entry.Timestamp.ToString("HH:mm:ss.fff"), GUILayout.Width(80));
            
            // 事件类型
            GUI.color = entry.Color;
            GUILayout.Label(entry.EventType, GUILayout.Width(150));
            
            // 发送者
            GUI.color = Color.cyan;
            GUILayout.Label(entry.SenderId, GUILayout.Width(100));
            
            // 详情
            GUI.color = Color.white;
            GUILayout.Label(entry.Message);
            
            GUILayout.EndHorizontal();
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// 格式化事件消息
        /// </summary>
        private string FormatEventMessage(IModEvent eventData)
        {
            if (eventData is ModErrorEvent errorEvent)
            {
                return $"{errorEvent.ErrorType}: {errorEvent.Message}";
            }
            
            // 处理特殊事件类型
            if (eventData.EventId == "system_ready")
            {
                var readyEvent = eventData as SystemReadyEvent;
                return $"System Ready - {readyEvent.LoadedModCount} mods loaded";
            }
            
            return eventData.ToString();
        }
        
        /// <summary>
        /// 获取事件颜色
        /// </summary>
        private Color GetEventColor(IModEvent eventData)
        {
            if (eventData is ModErrorEvent)
            {
                return Color.red;
            }
            
            switch (eventData.EventId)
            {
                case "system_ready":
                    return Color.green;
                case "mod_loaded":
                    return new Color(0.3f, 0.8f, 0.3f);
                case "mod_unloaded":
                    return new Color(0.8f, 0.3f, 0.3f);
                default:
                    return Color.white;
            }
        }
        
        /// <summary>
        /// 获取状态样式
        /// </summary>
        private GUIStyle GetStatusStyle()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.fontStyle = FontStyle.Bold;
            
            if (isPaused)
            {
                style.normal.textColor = Color.yellow;
            }
            
            return style;
        }
        
        /// <summary>
        /// 获取富文本样式
        /// </summary>
        private GUIStyle GetRichTextStyle()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.richText = true;
            return style;
        }
        
        /// <summary>
        /// 获取过滤后的事件
        /// </summary>
        private IEnumerable<EventLogEntry> GetFilteredEvents()
        {
            var filtered = eventHistory.AsEnumerable();
            
            // 应用错误过滤
            if (showOnlyErrors)
            {
                filtered = filtered.Where(e => e.IsError);
            }
            
            // 应用文本过滤
            if (!string.IsNullOrEmpty(filterText))
            {
                filtered = filtered.Where(e => 
                    e.EventType.Contains(filterText) || 
                    e.SenderId.Contains(filterText) || 
                    e.Message.Contains(filterText));
            }
            
            // 按时间降序排序
            return filtered.OrderByDescending(e => e.Timestamp);
        }
        
        /// <summary>
        /// 清除历史记录
        /// </summary>
        private void ClearHistory()
        {
            eventHistory.Clear();
            // 保留事件类型统计
        }
        
        /// <summary>
        /// 导出日志
        /// </summary>
        private void ExportLog()
        {
            // TODO: 实现日志导出功能
        }
        
        /// <summary>
        /// 计算事件速率
        /// </summary>
        private float CalculateEventRate()
        {
            // 简单实现：仅计算最近5秒内的事件数
            var now = DateTime.Now;
            var recentEvents = eventHistory.Count(e => (now - e.Timestamp).TotalSeconds <= 5);
            return recentEvents / 5f;
        }
        #endregion

        #region Data Structures
        /// <summary>
        /// 事件日志条目
        /// </summary>
        private class EventLogEntry
        {
            public string EventType { get; set; }
            public string SenderId { get; set; }
            public DateTime Timestamp { get; set; }
            public string Message { get; set; }
            public bool IsError { get; set; }
            public Color Color { get; set; } = Color.white;
        }
        #endregion
    }
} 