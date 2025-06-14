// UnityProject/Assets/Scripts/UnityImplementations/UnityGameObjectWrapper.cs
// 在文件顶部添加别名来区分不同的类型
using UnityEngine;
using ModSystem.Core;
using System;

// 使用别名来明确区分
using CoreVector3 = ModSystem.Core.Vector3;
using UnityVector3 = UnityEngine.Vector3;
using CoreQuaternion = ModSystem.Core.Quaternion;
using UnityQuaternion = UnityEngine.Quaternion;

namespace ModSystem.Unity
{
    /// <summary>
    /// Unity GameObject的包装器，实现IGameObject接口
    /// 将Unity的GameObject适配为平台无关的接口
    /// </summary>
    public class UnityGameObjectWrapper : IGameObject
    {
        #region Fields
        private readonly GameObject gameObject;
        private UnityTransformWrapper transformWrapper;
        #endregion

        #region Properties
        /// <summary>
        /// 获取原始的Unity GameObject
        /// </summary>
        public GameObject GameObject => gameObject;
        #endregion

        #region Constructor
        /// <summary>
        /// 创建GameObject包装器
        /// </summary>
        /// <param name="gameObject">要包装的GameObject</param>
        public UnityGameObjectWrapper(GameObject gameObject)
        {
            this.gameObject = gameObject ?? throw new ArgumentNullException(nameof(gameObject));
        }
        #endregion

        #region IGameObject Implementation
        /// <summary>
        /// 获取或设置对象名称
        /// </summary>
        public string Name
        {
            get => gameObject.name;
            set => gameObject.name = value;
        }
        
        /// <summary>
        /// 获取或设置对象是否启用
        /// </summary>
        public bool IsEnabled
        {
            get => gameObject.activeSelf;
            set => gameObject.SetActive(value);
        }
        
        /// <summary>
        /// 获取对象的标签
        /// </summary>
        public string Tag => gameObject.tag;
        
        /// <summary>
        /// 获取对象的变换
        /// </summary>
        public ITransform Transform
        {
            get
            {
                if (transformWrapper == null)
                {
                    transformWrapper = new UnityTransformWrapper(gameObject.transform);
                }
                return transformWrapper;
            }
        }
        
        /// <summary>
        /// 添加组件
        /// </summary>
        public T AddComponent<T>() where T : class
        {
            var componentType = typeof(T);
            
            // 如果T是Unity的组件类型，直接添加
            if (componentType.IsSubclassOf(typeof(Component)))
            {
                return gameObject.AddComponent(componentType) as T;
            }
            
            // 如果T是接口，尝试找到对应的Unity实现
            if (componentType.IsInterface)
            {
                // 这里可以添加接口到Unity组件的映射逻辑
                return null;
            }
            
            return null;
        }
        
        /// <summary>
        /// 获取组件
        /// </summary>
        public T GetComponent<T>() where T : class
        {
            var componentType = typeof(T);
            
            // 如果T是Unity的组件类型，直接获取
            if (componentType.IsSubclassOf(typeof(Component)))
            {
                return gameObject.GetComponent(componentType) as T;
            }
            
            // 如果T是接口，尝试找到对应的Unity实现
            if (componentType.IsInterface)
            {
                // 这里可以添加接口到Unity组件的映射逻辑
                return null;
            }
            
            return null;
        }
        
        /// <summary>
        /// 获取所有组件
        /// </summary>
        public T[] GetComponents<T>() where T : class
        {
            var componentType = typeof(T);
            
            // 如果T是Unity的组件类型，直接获取
            if (componentType.IsSubclassOf(typeof(Component)))
            {
                var components = gameObject.GetComponents(componentType);
                return System.Array.ConvertAll(components, c => c as T);
            }
            
            return new T[0];
        }
        
        /// <summary>
        /// 销毁对象
        /// </summary>
        public void Destroy()
        {
            Object.Destroy(gameObject);
        }
        #endregion
    }
    
    /// <summary>
    /// Unity Transform的包装器，实现ITransform接口
    /// </summary>
    public class UnityTransformWrapper : ITransform
    {
        #region Fields
        private readonly Transform transform;
        #endregion

        #region Constructor
        /// <summary>
        /// 创建Transform包装器
        /// </summary>
        /// <param name="transform">要包装的Transform</param>
        public UnityTransformWrapper(Transform transform)
        {
            this.transform = transform ?? throw new ArgumentNullException(nameof(transform));
        }
        #endregion

        #region ITransform Implementation
        /// <summary>
        /// 获取或设置位置
        /// </summary>
        public CoreVector3 Position
        {
            get => new CoreVector3(transform.position.x, transform.position.y, transform.position.z);
            set => transform.position = new UnityVector3(value.X, value.Y, value.Z);
        }
        
        /// <summary>
        /// 获取或设置旋转
        /// </summary>
        public CoreQuaternion Rotation
        {
            get
            {
                var rot = transform.rotation;
                return new CoreQuaternion(rot.x, rot.y, rot.z, rot.w);
            }
            set => transform.rotation = new UnityQuaternion(value.X, value.Y, value.Z, value.W);
        }
        
        /// <summary>
        /// 获取或设置缩放
        /// </summary>
        public CoreVector3 Scale
        {
            get => new CoreVector3(transform.localScale.x, transform.localScale.y, transform.localScale.z);
            set => transform.localScale = new UnityVector3(value.X, value.Y, value.Z);
        }
        
        /// <summary>
        /// 获取或设置父对象
        /// </summary>
        public ITransform Parent
        {
            get => transform.parent != null ? new UnityTransformWrapper(transform.parent) : null;
            set
            {
                if (value is UnityTransformWrapper wrapper)
                {
                    transform.SetParent(wrapper.transform);
                }
                else if (value == null)
                {
                    transform.SetParent(null);
                }
            }
        }
        
        /// <summary>
        /// 获取子对象数量
        /// </summary>
        public int ChildCount => transform.childCount;
        
        /// <summary>
        /// 获取前方向
        /// </summary>
        public CoreVector3 Forward => new CoreVector3(transform.forward.x, transform.forward.y, transform.forward.z);
        
        /// <summary>
        /// 获取上方向
        /// </summary>
        public CoreVector3 Up => new CoreVector3(transform.up.x, transform.up.y, transform.up.z);
        
        /// <summary>
        /// 获取右方向
        /// </summary>
        public CoreVector3 Right => new CoreVector3(transform.right.x, transform.right.y, transform.right.z);
        #endregion

        #region Methods
        /// <summary>
        /// 获取子对象
        /// </summary>
        public ITransform GetChild(int index)
        {
            if (index < 0 || index >= transform.childCount)
            {
                return null;
            }
            
            return new UnityTransformWrapper(transform.GetChild(index));
        }
        
        /// <summary>
        /// 查找子对象
        /// </summary>
        public ITransform Find(string name)
        {
            var child = transform.Find(name);
            return child != null ? new UnityTransformWrapper(child) : null;
        }
        
        /// <summary>
        /// 设置位置和旋转
        /// </summary>
        public void SetPositionAndRotation(CoreVector3 position, CoreQuaternion rotation)
        {
            transform.SetPositionAndRotation(
                new UnityVector3(position.X, position.Y, position.Z),
                new UnityQuaternion(rotation.X, rotation.Y, rotation.Z, rotation.W)
            );
        }
        
        /// <summary>
        /// 朝向目标点
        /// </summary>
        public void LookAt(CoreVector3 target)
        {
            transform.LookAt(new UnityVector3(target.X, target.Y, target.Z));
        }
        
        /// <summary>
        /// 旋转
        /// </summary>
        public void Rotate(CoreVector3 eulerAngles)
        {
            transform.Rotate(new UnityVector3(eulerAngles.X, eulerAngles.Y, eulerAngles.Z));
        }
        
        /// <summary>
        /// 平移
        /// </summary>
        public void Translate(CoreVector3 translation)
        {
            transform.Translate(new UnityVector3(translation.X, translation.Y, translation.Z));
        }
        #endregion
    }
}