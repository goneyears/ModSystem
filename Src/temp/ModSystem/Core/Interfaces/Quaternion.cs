using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 四元数结构，兼容Unity的Quaternion
    /// </summary>
    [Serializable]
    public struct Quaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;
        
        public Quaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
        
        public static Quaternion Identity => new Quaternion(0, 0, 0, 1);
        
        public static Quaternion Euler(float x, float y, float z)
        {
            // 简化的欧拉角转四元数实现
            return Identity; // 实际实现需要正确的数学计算
        }
    }
} 