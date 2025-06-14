// Src/ModSystem/ModSystem.Core/Math/Vector3.cs
using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 三维向量结构，兼容Unity的Vector3
    /// </summary>
    [Serializable]
    public struct Vector3
    {
        public float x;
        public float y;
        public float z;

        // 大写属性别名，保持兼容性
        public float X => x;
        public float Y => y;
        public float Z => z;

        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector3 Zero => new Vector3(0, 0, 0);
        public static Vector3 One => new Vector3(1, 1, 1);
        public static Vector3 Forward => new Vector3(0, 0, 1);
        public static Vector3 Back => new Vector3(0, 0, -1);
        public static Vector3 Up => new Vector3(0, 1, 0);
        public static Vector3 Down => new Vector3(0, -1, 0);
        public static Vector3 Left => new Vector3(-1, 0, 0);
        public static Vector3 Right => new Vector3(1, 0, 0);

        public static float Distance(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dy = a.y - b.y;
            var dz = a.z - b.z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static Vector3 operator *(Vector3 a, float d)
        {
            return new Vector3(a.x * d, a.y * d, a.z * d);
        }

        public static Vector3 operator /(Vector3 a, float d)
        {
            return new Vector3(a.x / d, a.y / d, a.z / d);
        }

        public override string ToString()
        {
            return $"({x}, {y}, {z})";
        }
    }
}