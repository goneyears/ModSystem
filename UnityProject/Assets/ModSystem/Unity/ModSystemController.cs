using System.IO;
using UnityEngine;
using ModSystem.Core.Runtime;
using ModSystem.Unity.Events;
using ModSystem.Unity.Reflection;
using ModSystem.Unity.Lifecycle;

namespace ModSystem.Unity
{
    /// <summary>
    /// Unity模组系统控制器 - V5版本，添加配置支持
    /// </summary>
    public class ModSystemController : MonoBehaviour
    {
        [SerializeField] private string modsFolder = "Mods";
        [SerializeField] private string configsFolder = "ModConfigs";  // V5新增
        [SerializeField] private bool loadOnStart = true;

        private ModManagerCore _modManager;
        private UnityAccessBridge _unityAccess;
        private ModUpdateRunner _updateRunner;
        private static ModSystemController _instance;

        public static ModSystemController Instance => _instance;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // 确保UI系统基础组件存在
            EnsureUISystem();

            // 初始化
            var logger = new UnityLogger();
            _unityAccess = new UnityAccessBridge();
            _modManager = new ModManagerCore(logger, _unityAccess);
            
            // V5新增：设置配置路径
            // V5新增：设置配置路径
            string configPath = Path.Combine(Application.streamingAssetsPath, modsFolder, configsFolder);
            _modManager.SetConfigPath(configPath);
            Debug.Log($"[ModSystemController] Config path: {configPath}");
            
            // 初始化事件桥接
            var bridge = gameObject.AddComponent<UnityEventBridge>();
            bridge.Initialize(_modManager.EventBus);
            
            // V4添加：初始化Update运行器
            _updateRunner = gameObject.AddComponent<ModUpdateRunner>();
            _updateRunner.Initialize(_modManager.LifecycleManager);
            
            Debug.Log("[ModSystemController] V5 - Configuration support enabled");
        }

        /// <summary>
        /// 确保UI系统必需的组件存在
        /// </summary>
        private void EnsureUISystem()
        {
            // EventSystem是Unity UI的核心组件，必须存在
            if (!GameObject.Find("EventSystem"))
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                DontDestroyOnLoad(eventSystem);
                UnityEngine.Debug.Log("[ModSystem] Created EventSystem");
            }
        }

        void Start()
        {
            if (loadOnStart)
            {
                LoadMods();
            }
        }

        public void LoadMods()
        {
            string path = Path.Combine(Application.streamingAssetsPath, modsFolder);
            _modManager.LoadModsFromDirectory(path);
            
            // V5添加：创建配置目录（如果不存在）
            // V5添加：创建配置目录（如果不存在）
            string configPath = Path.Combine(Application.streamingAssetsPath, modsFolder, configsFolder);
            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
                Debug.Log($"[ModSystemController] Created config directory: {configPath}");
            }
        }

        public Core.Interfaces.IEventBus GetEventBus() => _modManager.EventBus;
        public Core.Interfaces.IUnityAccess GetUnityAccess() => _unityAccess;
        public Core.Lifecycle.LifecycleManager GetLifecycleManager() => _modManager.LifecycleManager;

        void OnDestroy()
        {
            _modManager?.ShutdownAllMods();
        }
    }
}