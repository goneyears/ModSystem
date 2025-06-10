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
        
        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        
        public static Vector3 Zero => new Vector3(0, 0, 0);
        public static Vector3 One => new Vector3(1, 1, 1);
        
        public static float Distance(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dy = a.y - b.y;
            var dz = a.z - b.z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
} 