using System;
using System.Collections.Generic;
using System.Text;

namespace ModSystem.Core.Interfaces
{
    /// <summary>
    /// 日志接口
    /// </summary>
    public interface ILogger
    {
        void Log(string message);
        void LogWarning(string message);
        void LogError(string message);
    }
}