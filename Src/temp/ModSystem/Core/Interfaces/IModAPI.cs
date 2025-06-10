namespace ModSystem.Core
{
    /// <summary>
    /// 模组API接口
    /// 提供模组可用的各种功能
    /// </summary>
    public interface IModAPI
    {
        /// <summary>
        /// 请求响应管理器
        /// </summary>
        IRequestResponseManager RequestResponse { get; }
        
        /// <summary>
        /// 对象工厂
        /// </summary>
        IObjectFactory ObjectFactory { get; }
        
        /// <summary>
        /// 工具类
        /// </summary>
        IModUtilities Utilities { get; }
    }
} 