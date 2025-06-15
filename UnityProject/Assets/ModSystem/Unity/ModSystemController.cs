using System.IO;
using UnityEngine;
using ModSystem.Core.Runtime;
using IModLogger = ModSystem.Core.Interfaces.ILogger;  // 使用别名避免冲突

namespace ModSystem.Unity
{
    /// <summary>
    /// Unity模组系统控制器
    /// </summary>
    public class ModSystemController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string modsFolder = "Mods";
        [SerializeField] private bool loadOnStart = true;

        private ModManagerCore _modManager;
        private IModLogger _logger;

        // 单例模式
        private static ModSystemController _instance;
        public static ModSystemController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ModSystemController>();
                    if (_instance == null)
                    {
                        var go = new GameObject("ModSystemController");
                        _instance = go.AddComponent<ModSystemController>();
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // 初始化
            _logger = new UnityLogger();
            _modManager = new ModManagerCore(_logger);
            
            _logger.Log("ModSystem initialized");
        }

        private void Start()
        {
            if (loadOnStart)
            {
                LoadMods();
            }
        }

        /// <summary>
        /// 加载模组
        /// </summary>
        public void LoadMods()
        {
            string modsPath = GetModsPath();
            _logger.Log($"Loading mods from: {modsPath}");
            
            if (!Directory.Exists(modsPath))
            {
                Directory.CreateDirectory(modsPath);
                _logger.LogWarning($"Created mods directory: {modsPath}");
            }

            _modManager.LoadModsFromDirectory(modsPath);
        }

        /// <summary>
        /// 获取模组路径
        /// </summary>
        private string GetModsPath()
        {
            // 在编辑器中使用StreamingAssets
#if UNITY_EDITOR
            return Path.Combine(Application.streamingAssetsPath, modsFolder);
#else
            // 在构建版本中使用持久化数据路径
            return Path.Combine(Application.persistentDataPath, modsFolder);
#endif
        }

        private void OnDestroy()
        {
            if (_modManager != null)
            {
                _logger.Log("Shutting down ModSystem");
                _modManager.ShutdownAllMods();
            }
        }

        /// <summary>
        /// 获取已加载的模组数量
        /// </summary>
        public int GetLoadedModCount()
        {
            return _modManager?.GetLoadedModCount() ?? 0;
        }
    }
}