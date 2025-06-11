namespace ModSystem.Core
{
    /// <summary>
    /// 模组状态枚举
    /// </summary>
    public enum ModState
    {
        /// <summary>
        /// 未加载
        /// </summary>
        NotLoaded,
        
        /// <summary>
        /// 正在加载
        /// </summary>
        Loading,
        
        /// <summary>
        /// 已加载
        /// </summary>
        Loaded,
        
        /// <summary>
        /// 激活中
        /// </summary>
        Active,
        
        /// <summary>
        /// 已暂停
        /// </summary>
        Paused,
        
        /// <summary>
        /// 正在卸载
        /// </summary>
        Unloading,
        
        /// <summary>
        /// 错误状态
        /// </summary>
        Error
    }
} 