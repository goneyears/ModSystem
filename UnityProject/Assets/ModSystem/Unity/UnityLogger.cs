using UnityEngine;
using IModLogger = ModSystem.Core.Interfaces.ILogger;  // 使用别名避免冲突

namespace ModSystem.Unity
{
    /// <summary>
    /// Unity日志实现
    /// </summary>
    public class UnityLogger : IModLogger
    {
        private readonly string _prefix;

        public UnityLogger(string prefix = "[ModSystem]")
        {
            _prefix = prefix;
        }

        public void Log(string message)
        {
            Debug.Log($"{_prefix} {message}");
        }

        public void LogWarning(string message)
        {
            Debug.LogWarning($"{_prefix} {message}");
        }

        public void LogError(string message)
        {
            Debug.LogError($"{_prefix} {message}");
        }
    }
}