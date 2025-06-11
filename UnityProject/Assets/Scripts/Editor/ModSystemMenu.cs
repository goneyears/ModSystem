#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using ModSystem.Unity;
using System.IO;

namespace ModSystem.Unity.Editor
{
    /// <summary>
    /// 编辑器菜单扩展
    /// 提供模组系统相关的编辑器工具
    /// </summary>
    public static class ModSystemMenu
    {
        [MenuItem("ModSystem/Create ModSystem Controller")]
        private static void CreateModSystemController()
        {
            // 检查是否已存在控制器
            var existingController = Object.FindObjectOfType<ModSystemController>();
            if (existingController != null)
            {
                EditorUtility.DisplayDialog("ModSystem", "A ModSystemController already exists in the scene!", "OK");
                Selection.activeGameObject = existingController.gameObject;
                return;
            }
            
            // 创建新的控制器
            var gameObject = new GameObject("ModSystemController");
            gameObject.AddComponent<ModSystemController>();
            Selection.activeGameObject = gameObject;
            
            Debug.Log("ModSystemController created successfully!");
        }
        
        [MenuItem("ModSystem/Add Event Monitor")]
        private static void AddEventMonitor()
        {
            // 检查是否已存在控制器
            var controller = Object.FindObjectOfType<ModSystemController>();
            if (controller == null)
            {
                bool create = EditorUtility.DisplayDialog("ModSystem", 
                    "No ModSystemController found in the scene. Create one first?", "Yes", "No");
                
                if (create)
                {
                    CreateModSystemController();
                    controller = Object.FindObjectOfType<ModSystemController>();
                }
                else
                {
                    return;
                }
            }
            
            // 检查是否已存在监视器
            var existingMonitor = Object.FindObjectOfType<Debug.EventMonitor>();
            if (existingMonitor != null)
            {
                EditorUtility.DisplayDialog("ModSystem", "An EventMonitor already exists in the scene!", "OK");
                Selection.activeGameObject = existingMonitor.gameObject;
                return;
            }
            
            // 创建监视器
            var gameObject = new GameObject("EventMonitor");
            gameObject.AddComponent<Debug.EventMonitor>();
            gameObject.transform.SetParent(controller.transform);
            Selection.activeGameObject = gameObject;
            
            Debug.Log("EventMonitor added successfully!");
        }
        
        [MenuItem("ModSystem/Create Directories")]
        private static void CreateDirectories()
        {
            string streamingAssetsPath = Application.streamingAssetsPath;
            
            // 创建模组目录
            string modsPath = Path.Combine(streamingAssetsPath, "Mods");
            if (!Directory.Exists(modsPath))
            {
                Directory.CreateDirectory(modsPath);
            }
            
            // 创建配置目录
            string configPath = Path.Combine(streamingAssetsPath, "ModConfigs");
            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
            }
            
            // 创建包目录
            string packagesPath = Path.Combine(streamingAssetsPath, "ModPackages");
            if (!Directory.Exists(packagesPath))
            {
                Directory.CreateDirectory(packagesPath);
            }
            
            // 刷新资源视图
            AssetDatabase.Refresh();
            
            Debug.Log("ModSystem directories created in StreamingAssets!");
        }
        
        [MenuItem("ModSystem/Open Documentation")]
        private static void OpenDocumentation()
        {
            // 打开文档（可以修改为实际的文档URL）
            Application.OpenURL("https://docs.example.com/modsystem");
        }
        
        [MenuItem("ModSystem/About")]
        private static void ShowAbout()
        {
            EditorUtility.DisplayDialog("About ModSystem", 
                "ModSystem v1.0\n\n" +
                "A flexible modding framework for Unity games.\n\n" +
                "© 2023 Your Company", 
                "OK");
        }
    }
}
#endif 