using UnityEngine;
using System.Threading.Tasks;
using ModSystem.Core;
using System.Collections.Generic;
using System;

namespace ModSystem.Unity
{
    /// <summary>
    /// Unity对象工厂实现
    /// 负责从JSON定义创建Unity GameObject
    /// </summary>
    public class UnityObjectFactory : ObjectFactoryBase
    {
        #region Fields
        private readonly Dictionary<string, Shader> shaderCache;
        private readonly Dictionary<string, Material> materialCache;
        private readonly Dictionary<string, Mesh> meshCache;
        #endregion

        #region Constructor
        /// <summary>
        /// 创建Unity对象工厂
        /// </summary>
        public UnityObjectFactory() : base(Application.streamingAssetsPath, new UnityLogger())
        {
            shaderCache = new Dictionary<string, Shader>();
            materialCache = new Dictionary<string, Material>();
            meshCache = new Dictionary<string, Mesh>();
            
            // 预加载常用着色器
            PreloadShaders();
        }
        #endregion

        #region ObjectFactoryBase Implementation
        /// <summary>
        /// 从对象定义创建GameObject
        /// </summary>
        public override async Task<IGameObject> CreateObjectFromDefinitionAsync(ObjectDefinition definition)
        {
            // 创建GameObject
            var gameObject = new GameObject(definition.name ?? "ModObject");
            var wrapper = new UnityGameObjectWrapper(gameObject);
            
            // 处理每个组件定义
            foreach (var compDef in definition.components)
            {
                try
                {
                    await AddComponentAsync(wrapper, compDef);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Failed to add component {compDef.type}: {ex.Message}");
                }
            }
            
            return wrapper;
        }
        #endregion

        #region Component Creation
        /// <summary>
        /// 异步添加组件到GameObject
        /// </summary>
        private async Task AddComponentAsync(UnityGameObjectWrapper wrapper, ComponentDefinition compDef)
        {
            // 简化版实现，实际上需要处理各种类型的组件
            GameObject obj = wrapper.GameObject;
            
            switch (compDef.type)
            {
                case "Transform":
                    ConfigureTransform(obj.transform, compDef);
                    break;
                    
                case "BoxCollider":
                    var boxCollider = obj.AddComponent<BoxCollider>();
                    // 配置BoxCollider的属性
                    break;
                    
                case "SphereCollider":
                    var sphereCollider = obj.AddComponent<SphereCollider>();
                    // 配置SphereCollider的属性
                    break;
                    
                case "Rigidbody":
                    var rigidbody = obj.AddComponent<Rigidbody>();
                    // 配置Rigidbody的属性
                    break;
                    
                default:
                    // 尝试通过反射添加组件
                    await Task.Yield(); // 模拟异步操作
                    TryAddComponentByReflection(obj, compDef);
                    break;
            }
        }
        #endregion

        #region Configuration Methods
        /// <summary>
        /// 配置Transform组件
        /// </summary>
        private void ConfigureTransform(Transform transform, ComponentDefinition compDef)
        {
            // 位置
            var position = compDef.GetProperty<float[]>("position", new float[] { 0, 0, 0 });
            transform.position = new Vector3(position[0], position[1], position[2]);
            
            // 旋转
            var rotation = compDef.GetProperty<float[]>("rotation", new float[] { 0, 0, 0 });
            transform.rotation = Quaternion.Euler(rotation[0], rotation[1], rotation[2]);
            
            // 缩放
            var scale = compDef.GetProperty<float[]>("scale", new float[] { 1, 1, 1 });
            transform.localScale = new Vector3(scale[0], scale[1], scale[2]);
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// 预加载着色器
        /// </summary>
        private void PreloadShaders()
        {
            shaderCache["Standard"] = Shader.Find("Standard");
            shaderCache["Unlit/Color"] = Shader.Find("Unlit/Color");
            shaderCache["Unlit/Texture"] = Shader.Find("Unlit/Texture");
            shaderCache["Sprites/Default"] = Shader.Find("Sprites/Default");
            shaderCache["UI/Default"] = Shader.Find("UI/Default");
        }
        
        /// <summary>
        /// 尝试通过反射添加组件
        /// </summary>
        private void TryAddComponentByReflection(GameObject obj, ComponentDefinition compDef)
        {
            try
            {
                // 查找组件类型
                var componentType = Type.GetType(compDef.type);
                if (componentType == null)
                {
                    // 尝试在UnityEngine命名空间中查找
                    componentType = Type.GetType($"UnityEngine.{compDef.type}, UnityEngine");
                }
                
                if (componentType != null && componentType.IsSubclassOf(typeof(Component)))
                {
                    var component = obj.AddComponent(componentType);
                    
                    // 使用反射设置属性
                    if (compDef.properties != null)
                    {
                        foreach (var kvp in compDef.properties)
                        {
                            var property = componentType.GetProperty(kvp.Key);
                            if (property != null && property.CanWrite)
                            {
                                try
                                {
                                    var value = Convert.ChangeType(kvp.Value, property.PropertyType);
                                    property.SetValue(component, value);
                                }
                                catch
                                {
                                    // 忽略无法设置的属性
                                }
                            }
                        }
                    }
                    
                    logger.Log($"Added component {compDef.type} via reflection");
                }
                else
                {
                    logger.LogWarning($"Unknown component type: {compDef.type}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to add component via reflection: {ex.Message}");
            }
        }
        #endregion
    }
    
    /// <summary>
    /// 按钮点击事件
    /// </summary>
    public class ButtonClickEvent : IModEvent
    {
        public string EventId => "ui_button_click";
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
        public string ButtonName { get; set; }
        public string EventName { get; set; }
    }
} 