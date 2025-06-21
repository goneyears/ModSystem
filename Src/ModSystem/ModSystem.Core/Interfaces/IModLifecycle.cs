using System;

namespace ModSystem.Core.Interfaces
{
    /// <summary>
    /// 模组生命周期接口 - 简化版
    /// 提供可选的生命周期方法
    /// </summary>
    public interface IModLifecycle
    {
        /// <summary>
        /// 每帧调用
        /// </summary>
        /// <param name="deltaTime">距离上一帧的时间（秒）</param>
        void OnUpdate(float deltaTime);

        /// <summary>
        /// 固定时间间隔调用（用于物理计算）
        /// </summary>
        /// <param name="fixedDeltaTime">固定时间间隔（通常0.02秒）</param>
        void OnFixedUpdate(float fixedDeltaTime);

        /// <summary>
        /// 在Update之后调用
        /// </summary>
        /// <param name="deltaTime">距离上一帧的时间（秒）</param>
        void OnLateUpdate(float deltaTime);
    }
}