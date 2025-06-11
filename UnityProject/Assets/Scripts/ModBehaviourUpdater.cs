// ModSystem.Unity/ModBehaviourUpdater.cs
using UnityEngine;
using ModSystem.Core;
using System;

namespace ModSystem.Unity
{
    /// <summary>
    /// Unity组件，负责调用模组行为的Update方法
    /// 将Unity的生命周期事件转发给模组行为
    /// </summary>
    [AddComponentMenu("ModSystem/Mod Behaviour Updater")]
    public class ModBehaviourUpdater : MonoBehaviour
    {
        #region Fields
        private IModBehaviour behaviour;
        private float lastUpdateTime;
        private bool isInitialized;
        private float updateInterval = 0f; // 0表示每帧更新
        private float timeSinceLastUpdate = 0f;
        #endregion

        #region Properties
        /// <summary>
        /// 获取关联的模组行为
        /// </summary>
        public IModBehaviour Behaviour => behaviour;
        
        /// <summary>
        /// 获取或设置更新间隔（秒）
        /// </summary>
        public float UpdateInterval
        {
            get => updateInterval;
            set => updateInterval = Mathf.Max(0f, value);
        }
        
        /// <summary>
        /// 获取行为是否已初始化
        /// </summary>
        public bool IsInitialized => isInitialized;
        #endregion

        #region Initialization
        /// <summary>
        /// 初始化更新器
        /// </summary>
        /// <param name="behaviour">要更新的模组行为</param>
        public void Initialize(IModBehaviour behaviour)
        {
            if (behaviour == null)
            {
                Debug.LogError("[ModBehaviourUpdater] Cannot initialize with null behaviour");
                enabled = false;
                return;
            }
            
            this.behaviour = behaviour;
            lastUpdateTime = Time.time;
            isInitialized = true;
            
            Debug.Log($"[ModBehaviourUpdater] Initialized for behaviour: {behaviour.BehaviourId}");
        }
        #endregion

        #region Unity Lifecycle
        void Start()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[ModBehaviourUpdater] Not initialized, disabling component");
                enabled = false;
            }
        }
        
        void Update()
        {
            if (!isInitialized || behaviour == null)
                return;
            
            // 检查更新间隔
            if (updateInterval > 0f)
            {
                timeSinceLastUpdate += Time.deltaTime;
                if (timeSinceLastUpdate < updateInterval)
                    return;
                
                timeSinceLastUpdate = 0f;
            }
            
            // 计算实际的deltaTime
            float deltaTime = Time.time - lastUpdateTime;
            lastUpdateTime = Time.time;
            
            try
            {
                // 调用行为的更新方法
                behaviour.OnUpdate(deltaTime);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ModBehaviourUpdater] Error updating behaviour {behaviour.BehaviourId}: {ex}");
                
                // 发布错误事件
                PublishErrorEvent(ex);
            }
        }
        
        void OnDestroy()
        {
            if (behaviour != null)
            {
                try
                {
                    behaviour.OnDestroy();
                    Debug.Log($"[ModBehaviourUpdater] Destroyed behaviour: {behaviour.BehaviourId}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ModBehaviourUpdater] Error destroying behaviour {behaviour.BehaviourId}: {ex}");
                }
                
                behaviour = null;
            }
        }
        
        void OnEnable()
        {
            lastUpdateTime = Time.time;
            timeSinceLastUpdate = 0f;
            
            // 可以添加恢复逻辑
            if (behaviour != null)
            {
                Debug.Log($"[ModBehaviourUpdater] Enabled behaviour: {behaviour.BehaviourId}");
            }
        }
        
        void OnDisable()
        {
            // 可以添加暂停逻辑
            if (behaviour != null)
            {
                Debug.Log($"[ModBehaviourUpdater] Disabled behaviour: {behaviour.BehaviourId}");
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 设置更新速率（每秒更新次数）
        /// </summary>
        /// <param name="updatesPerSecond">每秒更新次数，0表示每帧更新</param>
        public void SetUpdateRate(float updatesPerSecond)
        {
            if (updatesPerSecond <= 0f)
            {
                UpdateInterval = 0f;
            }
            else
            {
                UpdateInterval = 1f / updatesPerSecond;
            }
        }
        
        /// <summary>
        /// 暂停更新
        /// </summary>
        public void PauseUpdates()
        {
            enabled = false;
        }
        
        /// <summary>
        /// 恢复更新
        /// </summary>
        public void ResumeUpdates()
        {
            enabled = true;
            lastUpdateTime = Time.time;
        }
        
        /// <summary>
        /// 强制立即更新
        /// </summary>
        public void ForceUpdate()
        {
            if (behaviour != null)
            {
                float deltaTime = Time.time - lastUpdateTime;
                lastUpdateTime = Time.time;
                
                try
                {
                    behaviour.OnUpdate(deltaTime);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ModBehaviourUpdater] Error in forced update: {ex}");
                }
            }
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// 发布错误事件
        /// </summary>
        private void PublishErrorEvent(Exception ex)
        {
            var controller = ModSystemController.Instance;
            if (controller != null && controller.EventBus != null)
            {
                controller.EventBus.Publish(new ModErrorEvent
                {
                    SenderId = behaviour?.BehaviourId ?? "Unknown",
                    ErrorType = "UpdateError",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }
        #endregion
    }
} 