using UnityEngine;
using ModSystem.Core.Lifecycle;

namespace ModSystem.Unity.Lifecycle
{
    /// <summary>
    /// Unity Update运行器 - 驱动模组的生命周期方法
    /// </summary>
    public class ModUpdateRunner : MonoBehaviour
    {
        private LifecycleManager _lifecycleManager;

        /// <summary>
        /// 初始化运行器
        /// </summary>
        public void Initialize(LifecycleManager lifecycleManager)
        {
            _lifecycleManager = lifecycleManager;
            Debug.Log("[ModUpdateRunner] Initialized");
        }

        // Unity生命周期方法
        void Update()
        {
            if (_lifecycleManager != null)
            {
                _lifecycleManager.UpdateAll(Time.deltaTime);
            }
        }

        void FixedUpdate()
        {
            if (_lifecycleManager != null)
            {
                _lifecycleManager.FixedUpdateAll(Time.fixedDeltaTime);
            }
        }

        void LateUpdate()
        {
            if (_lifecycleManager != null)
            {
                _lifecycleManager.LateUpdateAll(Time.deltaTime);
            }
        }

        void OnDestroy()
        {
            Debug.Log("[ModUpdateRunner] Destroyed");
        }
    }
}