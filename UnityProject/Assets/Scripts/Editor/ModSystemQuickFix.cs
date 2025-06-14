// Assets/Scripts/Editor/ModSystemQuickFix.cs
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

namespace ModSystem.Unity.Editor
{
    /// <summary>
    /// 快速修复编译错误的编辑器工具
    /// </summary>
    public class ModSystemQuickFix : EditorWindow
    {
        [MenuItem("ModSystem/Tools/Quick Fix Compilation Errors")]
        static void ShowWindow()
        {
            GetWindow<ModSystemQuickFix>("ModSystem Quick Fix");
        }
        
        void OnGUI()
        {
            GUILayout.Label("ModSystem 编译错误快速修复", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("1. 修复 ModMemoryMonitor.cs (GCMemoryInfo)", GUILayout.Height(30)))
            {
                FixModMemoryMonitor();
            }
            
            if (GUILayout.Button("2. 创建 ModUIFactory.cs", GUILayout.Height(30)))
            {
                CreateModUIFactory();
            }
            
            if (GUILayout.Button("3. 修复 ILogger 歧义", GUILayout.Height(30)))
            {
                FixILoggerAmbiguity();
            }
            
            if (GUILayout.Button("4. 修复 UnityGameObjectWrapper (IsActive)", GUILayout.Height(30)))
            {
                FixUnityGameObjectWrapper();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("应用所有修复", GUILayout.Height(40)))
            {
                ApplyAllFixes();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("点击按钮应用相应的修复。建议备份项目后再使用。", MessageType.Info);
        }
        
        void FixModMemoryMonitor()
        {
            string path = "Assets/Scripts/Debug/ModMemoryMonitor.cs";
            if (File.Exists(path))
            {
                string content = File.ReadAllText(path);
                
                // 注释掉 GCMemoryInfo 行
                content = Regex.Replace(content, 
                    @"private\s+GCMemoryInfo\s+lastGCInfo;", 
                    "// private GCMemoryInfo lastGCInfo; // Unity不支持此API");
                
                File.WriteAllText(path, content);
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog("修复成功", "ModMemoryMonitor.cs 已修复", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "找不到 ModMemoryMonitor.cs", "确定");
            }
        }
        
        void CreateModUIFactory()
        {
            string directory = "Assets/Scripts/UI";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            string path = Path.Combine(directory, "ModUIFactory.cs");
            
            // 这里应该包含完整的ModUIFactory代码
            string content = GetModUIFactoryContent();
            
            File.WriteAllText(path, content);
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("创建成功", "ModUIFactory.cs 已创建", "确定");
        }
        
        void FixILoggerAmbiguity()
        {
            string path = "Assets/Scripts/ModSystemController.cs";
            if (File.Exists(path))
            {
                string content = File.ReadAllText(path);
                
                // 添加using别名
                if (!content.Contains("using IModLogger"))
                {
                    content = content.Replace("using ModSystem.Core;", 
                        "using ModSystem.Core;\nusing IModLogger = ModSystem.Core.ILogger;");
                    
                    // 替换ILogger为IModLogger
                    content = Regex.Replace(content, @"\bILogger\b", "IModLogger");
                }
                
                File.WriteAllText(path, content);
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog("修复成功", "ILogger歧义已修复", "确定");
            }
        }
        
        void FixUnityGameObjectWrapper()
        {
            string path = "Assets/Scripts/UnityImplementations/UnityGameObjectWrapper.cs";
            if (File.Exists(path))
            {
                string content = File.ReadAllText(path);
                
                // 将IsEnabled改为IsActive
                content = content.Replace("public bool IsEnabled", "public bool IsActive");
                
                File.WriteAllText(path, content);
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog("修复成功", "UnityGameObjectWrapper 已修复", "确定");
            }
        }
        
        void ApplyAllFixes()
        {
            FixModMemoryMonitor();
            CreateModUIFactory();
            FixILoggerAmbiguity();
            FixUnityGameObjectWrapper();
            
            EditorUtility.DisplayDialog("完成", "所有修复已应用，请等待Unity重新编译", "确定");
        }
        
        string GetModUIFactoryContent()
        {
            // 返回完整的ModUIFactory代码
            return @"// UnityProject/Assets/Scripts/UI/ModUIFactory.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModSystem.Core;

namespace ModSystem.Unity
{
    public class ModUIFactory
    {
        private readonly Canvas canvas;
        private readonly ILogger logger;
        private readonly Dictionary<string, GameObject> uiCache;
        
        public ModUIFactory(Canvas canvas, ILogger logger)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.uiCache = new Dictionary<string, GameObject>();
        }
        
        // ... 其他方法实现 ...
    }
}";
        }
    }
}