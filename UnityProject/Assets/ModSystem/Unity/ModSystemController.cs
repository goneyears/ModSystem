using System.IO;
using UnityEngine;
using ModSystem.Core.Runtime;
using ModSystem.Unity.Events;

namespace ModSystem.Unity
{
    /// <summary>
    /// Unity模组系统控制器
    /// </summary>
    public class ModSystemController : MonoBehaviour
    {
        [SerializeField] private string modsFolder = "Mods";
        [SerializeField] private bool loadOnStart = true;

        private ModManagerCore _modManager;
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
            _modManager = new ModManagerCore(new UnityLogger());
            
            // 初始化事件桥接
            var bridge = gameObject.AddComponent<UnityEventBridge>();
            bridge.Initialize(_modManager.EventBus);
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
        }

        public Core.Interfaces.IEventBus GetEventBus() => _modManager.EventBus;

        void OnDestroy()
        {
            _modManager?.ShutdownAllMods();
        }
    }
}