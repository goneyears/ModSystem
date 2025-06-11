using UnityEngine;
using UnityEditor;
using ModSystem.Core;
using System.Linq;
using System.Collections.Generic;

namespace ModSystem.Unity.Editor
{
    /// <summary>
    /// ModSystemController的自定义Inspector
    /// </summary>
    [CustomEditor(typeof(ModSystemController))]
    public class ModSystemControllerInspector : UnityEditor.Editor
    {
        private ModSystemController controller;
        private bool showLoadedMods = true;
        private bool showEventBus = true;
        private bool showServices = true;
        private bool showDebugOptions = false;
        
        void OnEnable()
        {
            controller = (ModSystemController)target;
        }
        
        public override void OnInspectorGUI()
        {
            // 标题
            EditorGUILayout.LabelField("Mod System Controller", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 状态信息
            DrawStatusInfo();
            
            EditorGUILayout.Space();
            
            // 控制按钮
            DrawControlButtons();
            
            EditorGUILayout.Space();
            
            // 加载的模组
            showLoadedMods = EditorGUILayout.Foldout(showLoadedMods, "Loaded Mods");
            if (showLoadedMods)
            {
                DrawLoadedMods();
            }
            
            // 事件总线信息
            showEventBus = EditorGUILayout.Foldout(showEventBus, "Event Bus");
            if (showEventBus)
            {
                DrawEventBusInfo();
            }
            
            // 服务注册信息
            showServices = EditorGUILayout.Foldout(showServices, "Registered Services");
            if (showServices)
            {
                DrawServicesInfo();
            }
            
            // 调试选项
            showDebugOptions = EditorGUILayout.Foldout(showDebugOptions, "Debug Options");
            if (showDebugOptions)
            {
                DrawDebugOptions();
            }
            
            // 在播放模式下自动刷新
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
        
        /// <summary>
        /// 绘制状态信息
        /// </summary>
        private void DrawStatusInfo()
        {
            EditorGUILayout.BeginVertical("box");
            
            // 运行状态
            var isRunning = Application.isPlaying && controller.ModManagerCore != null;
            var statusColor = isRunning ? Color.green : Color.red;
            var statusText = isRunning ? "Running" : "Not Running";
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Status:", GUILayout.Width(60));
            
            var oldColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField(statusText, EditorStyles.boldLabel);
            GUI.color = oldColor;
            
            EditorGUILayout.EndHorizontal();
            
            if (isRunning)
            {
                var modCount = controller.ModManagerCore.GetLoadedMods().Count();
                EditorGUILayout.LabelField("Loaded Mods:", modCount.ToString());
                
                if (controller.EventBus is ModEventBus eventBus)
                {
                    var stats = eventBus.GetEventStatistics();
                    EditorGUILayout.LabelField("Event Types:", stats.Count.ToString());
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制控制按钮
        /// </summary>
        private void DrawControlButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (!Application.isPlaying)
            {
                if (GUILayout.Button("Enter Play Mode"))
                {
                    EditorApplication.EnterPlaymode();
                }
            }
            else
            {
                if (GUILayout.Button("Reload Mods"))
                {
                    // 实现重新加载逻辑
                    Debug.Log("Reloading mods...");
                }
                
                if (GUILayout.Button("Unload All"))
                {
                    // 实现卸载所有模组逻辑
                    Debug.Log("Unloading all mods...");
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制加载的模组列表
        /// </summary>
        private void DrawLoadedMods()
        {
            EditorGUILayout.BeginVertical("box");
            
            if (!Application.isPlaying || controller.ModManagerCore == null)
            {
                EditorGUILayout.LabelField("Enter Play Mode to see loaded mods");
            }
            else
            {
                var mods = controller.ModManagerCore.GetLoadedMods().ToList();
                
                if (mods.Count == 0)
                {
                    EditorGUILayout.LabelField("No mods loaded");
                }
                else
                {
                    foreach (var mod in mods)
                    {
                        DrawModInfo(mod);
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制单个模组信息
        /// </summary>
        private void DrawModInfo(ModInstance mod)
        {
            EditorGUILayout.BeginVertical("box");
            
            // 模组基本信息
            EditorGUILayout.LabelField($"{mod.LoadedMod.Manifest.name} v{mod.LoadedMod.Manifest.version}");
            
            // 状态
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("State:", GUILayout.Width(50));
            
            var stateColor = mod.State switch
            {
                ModState.Active => Color.green,
                ModState.Error => Color.red,
                ModState.Paused => Color.yellow,
                _ => Color.white
            };
            
            var oldColor = GUI.color;
            GUI.color = stateColor;
            EditorGUILayout.LabelField(mod.State.ToString());
            GUI.color = oldColor;
            
            EditorGUILayout.EndHorizontal();
            
            // 详细信息
            EditorGUILayout.LabelField($"ID: {mod.LoadedMod.Manifest.id}");
            EditorGUILayout.LabelField($"Author: {mod.LoadedMod.Manifest.author}");
            EditorGUILayout.LabelField($"Behaviours: {mod.LoadedMod.Behaviours.Count}");
            
            // 操作按钮
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Unload", GUILayout.Width(60)))
            {
                controller.ModManagerCore.UnloadMod(mod.LoadedMod.Manifest.id);
            }
            
            if (mod.State == ModState.Active)
            {
                if (GUILayout.Button("Pause", GUILayout.Width(60)))
                {
                    mod.State = ModState.Paused;
                }
            }
            else if (mod.State == ModState.Paused)
            {
                if (GUILayout.Button("Resume", GUILayout.Width(60)))
                {
                    mod.State = ModState.Active;
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制事件总线信息
        /// </summary>
        private void DrawEventBusInfo()
        {
            EditorGUILayout.BeginVertical("box");
            
            if (!Application.isPlaying || controller.EventBus == null)
            {
                EditorGUILayout.LabelField("Event Bus not available");
            }
            else if (controller.EventBus is ModEventBus eventBus)
            {
                var stats = eventBus.GetEventStatistics();
                
                if (stats.Count == 0)
                {
                    EditorGUILayout.LabelField("No event subscriptions");
                }
                else
                {
                    foreach (var kvp in stats.OrderByDescending(x => x.Value))
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(kvp.Key, GUILayout.Width(200));
                        EditorGUILayout.LabelField($"{kvp.Value} handlers");
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制服务信息
        /// </summary>
        private void DrawServicesInfo()
        {
            EditorGUILayout.BeginVertical("box");
            
            if (!Application.isPlaying || controller.ServiceRegistry == null)
            {
                EditorGUILayout.LabelField("Service Registry not available");
            }
            else if (controller.ServiceRegistry is ModServiceRegistry registry)
            {
                var stats = registry.GetServiceStatistics();
                
                if (stats.Count == 0)
                {
                    EditorGUILayout.LabelField("No services registered");
                }
                else
                {
                    foreach (var kvp in stats)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(kvp.Key, GUILayout.Width(200));
                        EditorGUILayout.LabelField($"{kvp.Value} instances");
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制调试选项
        /// </summary>
        private void DrawDebugOptions()
        {
            EditorGUILayout.BeginVertical("box");
            
            if (GUILayout.Button("Open Event Monitor"))
            {
                ModSystemMenu.OpenEventMonitor();
            }
            
            if (GUILayout.Button("Open Performance Profiler"))
            {
                ModSystemMenu.OpenPerformanceProfiler();
            }
            
            if (GUILayout.Button("Open Memory Monitor"))
            {
                ModSystemMenu.OpenMemoryMonitor();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Export Debug Report"))
            {
                ExportDebugReport();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 导出调试报告
        /// </summary>
        private void ExportDebugReport()
        {
            var report = "ModSystem Debug Report\n";
            report += $"Generated: {System.DateTime.Now}\n\n";
            
            if (Application.isPlaying && controller.ModManagerCore != null)
            {
                report += "Loaded Mods:\n";
                foreach (var mod in controller.ModManagerCore.GetLoadedMods())
                {
                    report += $"- {mod.LoadedMod.Manifest.name} v{mod.LoadedMod.Manifest.version} ({mod.State})\n";
                }
            }
            else
            {
                report += "System not running\n";
            }
            
            var path = EditorUtility.SaveFilePanel("Save Debug Report", "", "ModSystemDebugReport.txt", "txt");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, report);
                Debug.Log($"Debug report saved to: {path}");
            }
        }
    }
    
    /// <summary>
    /// ModManager的自定义Inspector
    /// </summary>
    [CustomEditor(typeof(ModManager))]
    public class ModManagerInspector : UnityEditor.Editor
    {
        private ModManager manager;
        private bool showUnityInstances = true;
        
        void OnEnable()
        {
            manager = (ModManager)target;
        }
        
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Mod Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see mod instances", MessageType.Info);
                return;
            }
            
            // Unity实例
            showUnityInstances = EditorGUILayout.Foldout(showUnityInstances, "Unity Instances");
            if (showUnityInstances)
            {
                DrawUnityInstances();
            }
        }
        
        /// <summary>
        /// 绘制Unity实例列表
        /// </summary>
        private void DrawUnityInstances()
        {
            EditorGUILayout.BeginVertical("box");
            
            if (manager.UnityInstances.Count == 0)
            {
                EditorGUILayout.LabelField("No Unity instances");
            }
            else
            {
                foreach (var kvp in manager.UnityInstances)
                {
                    DrawUnityInstance(kvp.Key, kvp.Value);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制单个Unity实例
        /// </summary>
        private void DrawUnityInstance(string modId, ModUnityInstance instance)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField(modId, EditorStyles.boldLabel);
            
            if (instance.Container != null)
            {
                EditorGUILayout.ObjectField("Container", instance.Container, typeof(GameObject), true);
                EditorGUILayout.LabelField($"GameObjects: {instance.GameObjects.Count}");
                EditorGUILayout.LabelField($"Active Objects: {instance.ActiveObjectCount}");
                
                if (instance.EstimatedMemoryUsage > 0)
                {
                    var memoryMB = instance.EstimatedMemoryUsage / 1024f / 1024f;
                    EditorGUILayout.LabelField($"Memory: {memoryMB:F2} MB");
                }
            }
            else
            {
                EditorGUILayout.LabelField("Container destroyed");
            }
            
            EditorGUILayout.EndVertical();
        }
    }
    
    /// <summary>
    /// ModBehaviourUpdater的自定义Inspector
    /// </summary>
    [CustomEditor(typeof(ModBehaviourUpdater))]
    public class ModBehaviourUpdaterInspector : UnityEditor.Editor
    {
        private ModBehaviourUpdater updater;
        
        void OnEnable()
        {
            updater = (ModBehaviourUpdater)target;
        }
        
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Mod Behaviour Updater", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            if (updater.Behaviour != null)
            {
                EditorGUILayout.LabelField("Behaviour ID:", updater.Behaviour.BehaviourId);
                EditorGUILayout.LabelField("Version:", updater.Behaviour.Version);
                EditorGUILayout.LabelField("Initialized:", updater.IsInitialized.ToString());
                
                EditorGUILayout.Space();
                
                // 更新间隔
                var newInterval = EditorGUILayout.FloatField("Update Interval", updater.UpdateInterval);
                if (newInterval != updater.UpdateInterval)
                {
                    updater.UpdateInterval = newInterval;
                }
                
                EditorGUILayout.Space();
                
                // 控制按钮
                EditorGUILayout.BeginHorizontal();
                
                if (updater.enabled)
                {
                    if (GUILayout.Button("Pause"))
                    {
                        updater.PauseUpdates();
                    }
                }
                else
                {
                    if (GUILayout.Button("Resume"))
                    {
                        updater.ResumeUpdates();
                    }
                }
                
                if (GUILayout.Button("Force Update"))
                {
                    updater.ForceUpdate();
                }
                
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("No behaviour attached", MessageType.Warning);
            }
        }
    }
} 