using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 变换接口，用于抽象Unity的Transform
    /// 提供位置、旋转和缩放控制
    /// </summary>
    public interface ITransform
    {
        /// <summary>
        /// 世界空间位置
        /// </summary>
        Vector3 Position { get; set; }
        
        /// <summary>
        /// 世界空间旋转
        /// </summary>
        Quaternion Rotation { get; set; }
        
        /// <summary>
        /// 局部缩放
        /// </summary>
        Vector3 Scale { get; set; }
        
        /// <summary>
        /// 父变换
        /// </summary>
        ITransform Parent { get; set; }
    }
} 