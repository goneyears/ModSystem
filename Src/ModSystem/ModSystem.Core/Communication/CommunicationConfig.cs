using System.Collections.Generic;

namespace ModSystem.Core
{
    /// <summary>
    /// 通信配置
    /// </summary>
    public class CommunicationConfig
    {
        public List<RouteConfig> Routes { get; set; } = new List<RouteConfig>();
        public List<WorkflowConfig> Workflows { get; set; } = new List<WorkflowConfig>();
        public RouterSettings Settings { get; set; } = new RouterSettings();
    }
    
    /// <summary>
    /// 路由配置
    /// </summary>
    public class RouteConfig
    {
        public string Name { get; set; }
        public string SourceEvent { get; set; }
        public List<ConditionConfig> Conditions { get; set; } = new List<ConditionConfig>();
        public List<ActionConfig> Actions { get; set; } = new List<ActionConfig>();
        public bool Enabled { get; set; } = true;
        public int Priority { get; set; } = 0;
    }
    
    /// <summary>
    /// 条件配置
    /// </summary>
    public class ConditionConfig
    {
        public string Property { get; set; }
        public string Operator { get; set; }
        public object Value { get; set; }
    }
    
    /// <summary>
    /// 动作配置
    /// </summary>
    public class ActionConfig
    {
        public string TargetMod { get; set; }
        public string EventType { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public int Delay { get; set; }
    }
    
    /// <summary>
    /// 工作流配置
    /// </summary>
    public class WorkflowConfig
    {
        public string Name { get; set; }
        public TriggerConfig Trigger { get; set; }
        public List<WorkflowStep> Steps { get; set; }
    }
    
    /// <summary>
    /// 触发器配置
    /// </summary>
    public class TriggerConfig
    {
        public string Event { get; set; }
        public List<ConditionConfig> Conditions { get; set; }
    }
    
    /// <summary>
    /// 工作流步骤
    /// </summary>
    public class WorkflowStep
    {
        public string Action { get; set; }
        public string Event { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public int Delay { get; set; }
        public int Timeout { get; set; }
    }
    
    /// <summary>
    /// 路由器设置
    /// </summary>
    public class RouterSettings
    {
        public bool EnableDebugLogging { get; set; } = false;
        public int MaxConcurrentActions { get; set; } = 10;
        public int DefaultActionTimeout { get; set; } = 5000;
    }
} 