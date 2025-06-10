// ModSystem.Unity/ModManager.cs
using UnityEngine;
using ModSystem.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace ModSystem.Unity
{
    /// <summary>
    /// Unity特定的ModManager包装器
    /// 处理Unity生命周期和GameObject管理，作为Core层ModManagerCore的Unity层扩展
    /// </summary>
    [AddComponentMenu("ModSystem/Mod Manager")]
    [RequireComponent(typeof(ModSystemController))]
    public class ModManager : MonoBehaviour
    {
        #region Fields
        private ModManagerCore core;
        private Dictionary<string, ModUnityInstance> unityInstances;
        private IObjectFactory objectFactory;
        private Transform modsContainer;
        #endregion

        #region Properties
        /// <summary>
        /// 获取所有Unity模组实例
        /// </summary>
        public IReadOnlyDictionary<string, ModUnityInstance> UnityInstances => unityInstances;
        
        /// <summary>
        /// 获取模组容器Transform
        /// </summary>
        public Transform ModsContainer => modsContainer;
        #endregion

        #region Initialization
        /// <summary>
        /// 初始化ModManager
        /// </summary>
        /// <param name="core">核心模组管理器</param>
        public void Initialize(ModManagerCore core)
        {
            this.core = core;
            unityInstances = new Dictionary<string, ModUnityInstance>();
            objectFactory = new UnityObjectFactory();
            
            // 创建模组容器
            CreateModsContainer();
            
            // 监听模组加载/卸载事件
            SubscribeToEvents();
            
            Debug.Log("[ModManager] Initialized");
        }
        
        /// <summary>
        /// 创建模组容器GameObject
        /// </summary>
        private void CreateModsContainer()
        {
            GameObject containerObj = new GameObject("Mods");
            containerObj.transform.SetParent(transform);
            modsContainer = containerObj.transform;
        }
        
        /// <summary>
        /// 订阅核心事件
        /// </summary>
        private void SubscribeToEvents()
        {
            core.EventBus.Subscribe<ModLoadedEvent>(OnModLoaded);
            core.EventBus.Subscribe<ModUnloadedEvent>(OnModUnloaded);
            core.EventBus.Subscribe<CreateGameObjectRequest>(OnCreateGameObjectRequest);
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// 处理模组加载事件
        /// </summary>
        private void OnModLoaded(ModLoadedEvent e)
        {
            CreateUnityInstance(e.ModId);
        }
        
        /// <summary>
        /// 处理模组卸载事件
        /// </summary>
        private void OnModUnloaded(ModUnloadedEvent e)
        {
            DestroyUnityInstance(e.ModId);
        }
        
        /// <summary>
        /// 处理创建GameObject请求
        /// </summary>
        private async void OnCreateGameObjectRequest(CreateGameObjectRequest request)
        {
            try
            {
                var gameObject = await CreateGameObjectFromDefinition(request.Definition);
                
                // 发送响应
                core.EventBus.Publish(new CreateGameObjectResponse
                {
                    RequestId = request.RequestId,
                    Success = true,
                    GameObject = new UnityGameObjectWrapper(gameObject)
                });
            }
            catch (Exception ex)
            {
                core.EventBus.Publish(new CreateGameObjectResponse
                {
                    RequestId = request.RequestId,
                    Success = false,
                    Message = ex.Message
                });
            }
        }
        #endregion

        #region Unity Instance Management
        /// <summary>
        /// 创建模组的Unity实例
        /// </summary>
        private void CreateUnityInstance(string modId)
        {
            var modInstance = core.GetModInstance(modId);
            if (modInstance == null) 
            {
                Debug.LogError($"[ModManager] Mod instance not found: {modId}");
                return;
            }
            
            // 创建Unity容器
            var container = new GameObject($"Mod_{modId}");
            container.transform.SetParent(modsContainer);
            
            var unityInstance = new ModUnityInstance
            {
                Container = container,
                GameObjects = new List<GameObject>(),
                Components = new List<MonoBehaviour>()
            };
            
            // 为每个行为创建GameObject
            CreateBehaviourGameObjects(modInstance, unityInstance);
            
            // 创建对象定义中的GameObject
            CreateObjectsFromDefinitions(modInstance, unityInstance);
            
            unityInstances[modId] = unityInstance;
            
            Debug.Log($"[ModManager] Created Unity instance for mod: {modId}");
        }
        
        /// <summary>
        /// 为模组行为创建GameObject
        /// </summary>
        private void CreateBehaviourGameObjects(ModInstance modInstance, ModUnityInstance unityInstance)
        {
            foreach (var behaviour in modInstance.LoadedMod.Behaviours)
            {
                var behaviourObj = new GameObject($"Behaviour_{behaviour.BehaviourId}");
                behaviourObj.transform.SetParent(unityInstance.Container.transform);
                
                // 添加更新组件
                var updater = behaviourObj.AddComponent<ModBehaviourUpdater>();
                updater.Initialize(behaviour);
                
                // 设置模组上下文中的GameObject
                if (core.GetModInstance(modInstance.LoadedMod.Manifest.id) is ModInstance instance)
                {
                    // 这里需要通过某种方式设置behaviour的GameObject引用
                    // 可能需要扩展IModBehaviour接口或使用反射
                }
                
                unityInstance.GameObjects.Add(behaviourObj);
                unityInstance.Components.Add(updater);
            }
        }
        
        /// <summary>
        /// 从对象定义创建GameObject
        /// </summary>
        private async void CreateObjectsFromDefinitions(ModInstance modInstance, ModUnityInstance unityInstance)
        {
            foreach (var objDef in modInstance.LoadedMod.Resources.ObjectDefinitions.Values)
            {
                try
                {
                    var obj = await CreateGameObjectFromDefinition(objDef);
                    obj.transform.SetParent(unityInstance.Container.transform);
                    unityInstance.GameObjects.Add(obj);
                    
                    Debug.Log($"[ModManager] Created object: {objDef.objectId}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ModManager] Failed to create object {objDef.objectId}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 从定义创建GameObject
        /// </summary>
        private async Task<GameObject> CreateGameObjectFromDefinition(ObjectDefinition definition)
        {
            var wrapper = await objectFactory.CreateObjectFromDefinitionAsync(definition);
            if (wrapper is UnityGameObjectWrapper unityWrapper)
            {
                return unityWrapper.GameObject;
            }
            
            throw new InvalidOperationException("Failed to create GameObject from definition");
        }
        
        /// <summary>
        /// 销毁Unity实例
        /// </summary>
        private void DestroyUnityInstance(string modId)
        {
            if (unityInstances.TryGetValue(modId, out var instance))
            {
                // 清理所有组件
                foreach (var component in instance.Components)
                {
                    if (component != null)
                    {
                        Destroy(component);
                    }
                }
                
                // 销毁容器GameObject
                if (instance.Container != null)
                {
                    Destroy(instance.Container);
                }
                
                unityInstances.Remove(modId);
                
                Debug.Log($"[ModManager] Destroyed Unity instance for mod: {modId}");
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 获取指定模组的Unity实例
        /// </summary>
        public ModUnityInstance GetUnityInstance(string modId)
        {
            return unityInstances.TryGetValue(modId, out var instance) ? instance : null;
        }
        
        /// <summary>
        /// 获取所有已加载的模组信息
        /// </summary>
        public IEnumerable<ModInstance> GetLoadedMods()
        {
            return core.GetLoadedMods();
        }
        
        /// <summary>
        /// 查找模组中的GameObject
        /// </summary>
        public GameObject FindModGameObject(string modId, string objectName)
        {
            if (unityInstances.TryGetValue(modId, out var instance))
            {
                return instance.GameObjects.FirstOrDefault(go => go != null && go.name == objectName);
            }
            return null;
        }
        
        /// <summary>
        /// 获取模组中的所有组件
        /// </summary>
        public T[] GetModComponents<T>(string modId) where T : Component
        {
            if (unityInstances.TryGetValue(modId, out var instance))
            {
                return instance.Container.GetComponentsInChildren<T>();
            }
            return new T[0];
        }
        #endregion

        #region Unity Lifecycle
        void OnDestroy()
        {
            // 清理所有Unity实例
            foreach (var modId in unityInstances.Keys.ToList())
            {
                DestroyUnityInstance(modId);
            }
            
            // 取消事件订阅
            if (core != null)
            {
                core.EventBus.UnsubscribeAll(this);
            }
        }
        #endregion
    }

    #region Data Structures
    /// <summary>
    /// 模组的Unity实例数据
    /// </summary>
    public class ModUnityInstance
    {
        /// <summary>
        /// 模组的根容器GameObject
        /// </summary>
        public GameObject Container { get; set; }
        
        /// <summary>
        /// 模组创建的所有GameObject列表
        /// </summary>
        public List<GameObject> GameObjects { get; set; }
        
        /// <summary>
        /// 模组的所有MonoBehaviour组件
        /// </summary>
        public List<MonoBehaviour> Components { get; set; }
        
        /// <summary>
        /// 获取活动的GameObject数量
        /// </summary>
        public int ActiveObjectCount => GameObjects?.Count(go => go != null && go.activeInHierarchy) ?? 0;
        
        /// <summary>
        /// 获取内存使用估算（字节）
        /// </summary>
        public long EstimatedMemoryUsage { get; set; }
    }
    
    /// <summary>
    /// 创建GameObject请求
    /// </summary>
    public class CreateGameObjectRequest : ModRequest
    {
        public ObjectDefinition Definition { get; set; }
    }
    
    /// <summary>
    /// 创建GameObject响应
    /// </summary>
    public class CreateGameObjectResponse : ModResponse
    {
        public IGameObject GameObject { get; set; }
    }
    #endregion
} 