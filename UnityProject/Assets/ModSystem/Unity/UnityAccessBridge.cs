using System;
using System.Reflection;
using System.Linq;
using ModSystem.Core.Interfaces;

namespace ModSystem.Unity.Reflection
{
    /// <summary>
    /// Unity访问桥接实现 - 简化版
    /// </summary>
    public class UnityAccessBridge : IUnityAccess
    {
        public bool IsUnityEnvironment => true;

        public Assembly[] GetUnityAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName.Contains("Unity"))
                .ToArray();
        }
    }
}