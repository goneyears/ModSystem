namespace ModSystem.Core
{
    /// <summary>
    /// 用于存储对象行为引用的组件
    /// </summary>
    public class ObjectBehaviourComponent
    {
        /// <summary>
        /// 关联的行为实例
        /// </summary>
        public IObjectBehaviour Behaviour { get; set; }
    }
} 