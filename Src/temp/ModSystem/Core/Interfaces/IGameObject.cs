using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 游戏对象接口，用于抽象Unity的GameObject
    /// 提供平台无关的对象操作
    /// </summary>
    public interface IGameObject
    {
        /// <summary>
        /// 对象名称
        /// </summary>
        string Name { get; set; }
        
        /// <summary>
        /// 对象是否激活
        /// </summary>
        bool IsActive { get; set; }
        
        /// <summary>
        /// 对象的变换组件
        /// </summary>
        ITransform Transform { get; }
        
        /// <summary>
        /// 获取指定类型的组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件实例，如果不存在则返回null</returns>
        T GetComponent<T>() where T : class;
        
        /// <summary>
        /// 添加指定类型的组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>新添加的组件实例</returns>
        T AddComponent<T>() where T : class;
    }
} 