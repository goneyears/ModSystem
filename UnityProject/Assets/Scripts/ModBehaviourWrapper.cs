// UnityProject/Assets/Scripts/ModBehaviourWrapper.cs
using UnityEngine;
using ModSystem.Core;
using System;

namespace ModSystem.Unity
{
    /// <summary>
    /// IMod接口的Unity包装器
    /// 将IMod适配到Unity的生命周期
    /// </summary>
    [AddComponentMenu("ModSystem/Mod Behaviour Wrapper")]
    public class ModBehaviourWrapper : MonoBehaviour
    {
        #region Fields
        private IMod modBehaviour;
        private ModInstance modInstance;
        private bool isInitialized;
        private float updateInterval = 0f;
        private float timeSinceLastUpdate = 0f;
        #endregion
        
        #region Properties
        /// <summary>
        /// 获取包装的模组行为
        /// </summary>
        public IMod ModBehaviour => modBehaviour;
        
        /// <summary>
        /// 获取模组实例
        /// </summary>
        public ModInstance ModInstance => modInstance;
        
        /// <summary>
        /// 获取或设置更新间隔（秒）
        /// </summary>
        public float UpdateInterval
        {
            get => updateInterval;
            set => updateInterval = Mathf.Max(0f, value);
        }
        
        /// <summary>
        /// 获取是否已初始化
        /// </summary>
        public bool IsInitialized => isInitialized;
        #endregion
        
        #region Initialization
        /// <summary>
        /// 初始化包装器
        /// </summary>
        public void Initialize(IMod behaviour, ModInstance instance)
        {
            if (behaviour == null)
            {
                Debug.LogError("[ModBehaviourWrapper] Cannot initialize with null behaviour");
                enabled = false;
                return;
            }
            
            this.modBehaviour = behaviour;
            this.modInstance = instance;
            this.isInitialized = true;
            
            Debug.Log($"[ModBehaviourWrapper] Initialized for mod: {instance.LoadedMod.Manifest.id}");
        }
        #endregion
        
        #region Unity Lifecycle
        void Start()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[ModBehaviourWrapper] Not initialized, disabling component");
                enabled = false;
            }
        }
        
        void Update()
        {
            // IMod接口没有Update方法，但可以用于未来扩展
            // 或者如果模组实现了其他可更新接口
            if (!isInitialized || modBehaviour == null)
                return;
                
            // 检查更新间隔
            if (updateInterval > 0f)
            {
                timeSinceLastUpdate += Time.deltaTime;
                if (timeSinceLastUpdate < updateInterval)
                    return;
                    
                timeSinceLastUpdate = 0f;
            }
            
            // 如果模组实现了可更新接口，在这里调用
            // 例如：if (modBehaviour is IUpdatable updatable) { updatable.Update(Time.deltaTime); }
        }
        
        void OnDisable()
        {
            if (isInitialized && modBehaviour != null)
            {
                try
                {
                    modBehaviour.OnDisable();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ModBehaviourWrapper] Error in OnDisable: {ex.Message}");
                }
            }
        }
        
        void OnDestroy()
        {
            if (isInitialized && modBehaviour != null)
            {
                try
                {
                    // 如果支持热重载，先调用OnBeforeReload
                    if (modBehaviour is IReloadable reloadable)
                    {
                        reloadable.OnBeforeReload();
                    }
                    
                    modBehaviour.OnDestroy();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ModBehaviourWrapper] Error in OnDestroy: {ex.Message}");
                }
            }
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            if (isInitialized && modBehaviour != null)
            {
                // 可以在这里处理暂停/恢复逻辑
                if (pauseStatus)
                {
                    // 应用暂停
                    if (modBehaviour is IReloadable reloadable)
                    {
                        try
                        {
                            reloadable.OnBeforeReload();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[ModBehaviourWrapper] Error in pause handling: {ex.Message}");
                        }
                    }
                }
                else
                {
                    // 应用恢复
                    if (modBehaviour is IReloadable reloadable)
                    {
                        try
                        {
                            reloadable.OnAfterReload();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[ModBehaviourWrapper] Error in resume handling: {ex.Message}");
                        }
                    }
                }
            }
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// 手动触发重载前回调
        /// </summary>
        public void TriggerBeforeReload()
        {
            if (modBehaviour is IReloadable reloadable)
            {
                try
                {
                    reloadable.OnBeforeReload();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ModBehaviourWrapper] Error in OnBeforeReload: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 手动触发重载后回调
        /// </summary>
        public void TriggerAfterReload()
        {
            if (modBehaviour is IReloadable reloadable)
            {
                try
                {
                    reloadable.OnAfterReload();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ModBehaviourWrapper] Error in OnAfterReload: {ex.Message}");
                }
            }
        }
        #endregion
    }
}