using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ModSystem.Core
{
    /// <summary>
    /// 抽象对象工厂基类
    /// </summary>
    public abstract class ObjectFactoryBase : IObjectFactory
    {
        protected readonly Dictionary<string, ObjectDefinition> definitionCache;
        protected readonly string basePath;
        protected readonly ILogger logger;
        
        /// <summary>
        /// 创建对象工厂
        /// </summary>
        protected ObjectFactoryBase(string basePath, ILogger logger)
        {
            this.basePath = basePath;
            this.logger = logger;
            definitionCache = new Dictionary<string, ObjectDefinition>();
        }
        
        /// <summary>
        /// 从定义文件创建对象
        /// </summary>
        public async Task<IGameObject> CreateObjectAsync(string definitionPath)
        {
            ObjectDefinition definition;
            
            if (definitionCache.ContainsKey(definitionPath))
            {
                definition = definitionCache[definitionPath];
            }
            else
            {
                var json = await LoadJsonAsync(definitionPath);
                definition = JsonConvert.DeserializeObject<ObjectDefinition>(json);
                definitionCache[definitionPath] = definition;
            }
            
            return await CreateObjectFromDefinitionAsync(definition);
        }
        
        /// <summary>
        /// 从对象定义创建对象（由子类实现）
        /// </summary>
        public abstract Task<IGameObject> CreateObjectFromDefinitionAsync(ObjectDefinition definition);
        
        /// <summary>
        /// 加载JSON文件
        /// </summary>
        protected virtual async Task<string> LoadJsonAsync(string path)
        {
            var fullPath = System.IO.Path.Combine(basePath, path);
            return await System.IO.File.ReadAllTextAsync(fullPath);
        }
        
        /// <summary>
        /// 配置对象行为
        /// </summary>
        protected virtual void ConfigureObjectBehaviour(IGameObject obj, ComponentDefinition compDef)
        {
            var behaviourClass = compDef.GetProperty<string>("behaviourClass");
            if (string.IsNullOrEmpty(behaviourClass))
            {
                logger.LogError("ObjectBehaviour requires behaviourClass property");
                return;
            }
            
            var behaviourType = Type.GetType(behaviourClass);
            if (behaviourType != null && typeof(IObjectBehaviour).IsAssignableFrom(behaviourType))
            {
                var behaviour = Activator.CreateInstance(behaviourType) as IObjectBehaviour;
                
                // 附加到对象
                behaviour.OnAttach(obj);
                
                // 配置行为
                var config = compDef.GetProperty<Dictionary<string, object>>("config");
                if (config != null)
                {
                    behaviour.OnConfigure(config);
                }
                
                // 存储引用
                var component = obj.AddComponent<ObjectBehaviourComponent>();
                if (component != null)
                {
                    component.Behaviour = behaviour;
                }
            }
            else
            {
                logger.LogError($"Could not find or instantiate behaviour class: {behaviourClass}");
            }
        }
    }
} 