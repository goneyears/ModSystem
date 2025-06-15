using UnityEngine;
using ModSystem.Core.Interfaces;

namespace ModSystem.Unity
{
    /// <summary>
    /// Unity日志实现
    /// </summary>
    public class UnityLogger : ModSystem.Core.Interfaces.ILogger
    {
        public void Log(string message) => UnityEngine.Debug.Log($"[Mod] {message}");
        public void LogWarning(string message) => UnityEngine.Debug.LogWarning($"[Mod] {message}");
        public void LogError(string message) => UnityEngine.Debug.LogError($"[Mod] {message}");
    }
}