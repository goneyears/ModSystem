using System;
using System.Collections.Generic;
using System.Text;

namespace ModSystem.Core.Runtime
{
    /// <summary>
    /// 模组信息
    /// </summary>
    public class ModInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string AssemblyPath { get; set; }
        public string MainClass { get; set; }
    }
}