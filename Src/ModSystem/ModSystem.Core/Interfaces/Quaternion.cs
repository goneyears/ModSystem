// Src/ModSystem/ModSystem.Core/Math/Quaternion.cs
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

        // 大写属性别名，保持兼容性
        public float X => x;
        public float Y => y;
        public float Z => z;
        public float W => w;

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
            float cx = (float)Math.Cos(x * 0.5f * Math.PI / 180f);
            float sx = (float)Math.Sin(x * 0.5f * Math.PI / 180f);
            float cy = (float)Math.Cos(y * 0.5f * Math.PI / 180f);
            float sy = (float)Math.Sin(y * 0.5f * Math.PI / 180f);
            float cz = (float)Math.Cos(z * 0.5f * Math.PI / 180f);
            float sz = (float)Math.Sin(z * 0.5f * Math.PI / 180f);

            return new Quaternion(
                sx * cy * cz - cx * sy * sz,
                cx * sy * cz + sx * cy * sz,
                cx * cy * sz - sx * sy * cz,
                cx * cy * cz + sx * sy * sz
            );
        }

        public static Quaternion Euler(Vector3 euler)
        {
            return Euler(euler.x, euler.y, euler.z);
        }

        public override string ToString()
        {
            return $"({x}, {y}, {z}, {w})";
        }
    }
}