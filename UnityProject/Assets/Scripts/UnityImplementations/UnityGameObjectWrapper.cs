// ModSystem.Unity/UnityImplementations/UnityGameObjectWrapper.cs
using UnityEngine;
using ModSystem.Core;
using System;

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
            // 这里需要处理类型转换和适配
            if (typeof(MonoBehaviour).IsAssignableFrom(typeof(T)))
            {
                return gameObject.AddComponent(typeof(T)) as T;
            }
            return null;
        }
        
        /// <summary>
        /// 获取组件
        /// </summary>
        public T GetComponent<T>() where T : class
        {
            // 这里需要处理类型转换和适配
            if (typeof(MonoBehaviour).IsAssignableFrom(typeof(T)))
            {
                return gameObject.GetComponent(typeof(T)) as T;
            }
            return null;
        }
        
        /// <summary>
        /// 获取所有组件
        /// </summary>
        public T[] GetComponents<T>() where T : class
        {
            // 这里需要处理类型转换和适配
            if (typeof(MonoBehaviour).IsAssignableFrom(typeof(T)))
            {
                return gameObject.GetComponents(typeof(T)) as T[];
            }
            return new T[0];
        }
        
        /// <summary>
        /// 销毁对象
        /// </summary>
        public void Destroy()
        {
            UnityEngine.Object.Destroy(gameObject);
        }
        #endregion

        #region Comparison
        /// <summary>
        /// 判断对象是否相等
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is UnityGameObjectWrapper other)
            {
                return gameObject == other.gameObject;
            }
            return false;
        }
        
        /// <summary>
        /// 获取哈希码
        /// </summary>
        public override int GetHashCode()
        {
            return gameObject.GetHashCode();
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
        public Vector3 Position
        {
            get => new Vector3(transform.position.x, transform.position.y, transform.position.z);
            set => transform.position = new UnityEngine.Vector3(value.X, value.Y, value.Z);
        }
        
        /// <summary>
        /// 获取或设置旋转
        /// </summary>
        public Quaternion Rotation
        {
            get
            {
                var rot = transform.rotation;
                return new Quaternion(rot.x, rot.y, rot.z, rot.w);
            }
            set => transform.rotation = new UnityEngine.Quaternion(value.X, value.Y, value.Z, value.W);
        }
        
        /// <summary>
        /// 获取或设置缩放
        /// </summary>
        public Vector3 Scale
        {
            get => new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.z);
            set => transform.localScale = new UnityEngine.Vector3(value.X, value.Y, value.Z);
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
        public Vector3 Forward => new Vector3(transform.forward.x, transform.forward.y, transform.forward.z);
        
        /// <summary>
        /// 获取上方向
        /// </summary>
        public Vector3 Up => new Vector3(transform.up.x, transform.up.y, transform.up.z);
        
        /// <summary>
        /// 获取右方向
        /// </summary>
        public Vector3 Right => new Vector3(transform.right.x, transform.right.y, transform.right.z);
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
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(
                new UnityEngine.Vector3(position.X, position.Y, position.Z),
                new UnityEngine.Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W)
            );
        }
        
        /// <summary>
        /// 朝向目标点
        /// </summary>
        public void LookAt(Vector3 target)
        {
            transform.LookAt(new UnityEngine.Vector3(target.X, target.Y, target.Z));
        }
        
        /// <summary>
        /// 旋转
        /// </summary>
        public void Rotate(Vector3 eulerAngles)
        {
            transform.Rotate(new UnityEngine.Vector3(eulerAngles.X, eulerAngles.Y, eulerAngles.Z));
        }
        
        /// <summary>
        /// 平移
        /// </summary>
        public void Translate(Vector3 translation)
        {
            transform.Translate(new UnityEngine.Vector3(translation.X, translation.Y, translation.Z));
        }
        #endregion
    }
} 