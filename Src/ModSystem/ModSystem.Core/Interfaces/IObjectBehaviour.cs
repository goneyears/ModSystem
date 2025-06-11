using System.Collections.Generic;

namespace ModSystem.Core
{
    /// <summary>
    /// 对象附加行为接口
    /// 用于为通过ObjectFactory创建的对象添加自定义行为
    /// </summary>
    public interface IObjectBehaviour
    {
        /// <summary>
        /// 当行为附加到游戏对象时调用
        /// </summary>
        /// <param name="gameObject">目标游戏对象</param>
        void OnAttach(IGameObject gameObject);
        
        /// <summary>
        /// 配置行为参数
        /// </summary>
        /// <param name="config">配置参数字典</param>
        void OnConfigure(Dictionary<string, object> config);
        
        /// <summary>
        /// 当行为从游戏对象分离时调用
        /// </summary>
        void OnDetach();
    }
} 