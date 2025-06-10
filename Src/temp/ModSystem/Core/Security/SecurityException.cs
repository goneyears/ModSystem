using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 安全异常
    /// </summary>
    public class SecurityException : System.Exception
    {
        public SecurityException(string message) : base(message) { }
        public SecurityException(string message, System.Exception inner) : base(message, inner) { }
    }
} 