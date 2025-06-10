namespace ModSystem.Core
{
    /// <summary>
    /// 模组行为接口
    /// 定义模组的主要逻辑和生命周期方法
    /// </summary>
    public interface IModBehaviour
    {
        /// <summary>
        /// 行为的唯一标识符
        /// </summary>
        string BehaviourId { get; }
        
        /// <summary>
        /// 行为版本号
        /// </summary>
        string Version { get; }
        
        /// <summary>
        /// 初始化方法，在模组加载时调用
        /// </summary>
        /// <param name="context">模组上下文</param>
        void OnInitialize(IModContext context);
        
        /// <summary>
        /// 更新方法，每帧调用
        /// </summary>
        /// <param name="deltaTime">自上次更新以来的时间（秒）</param>
        void OnUpdate(float deltaTime);
        
        /// <summary>
        /// 销毁方法，在模组卸载时调用
        /// </summary>
        void OnDestroy();
    }
} 