namespace ModSystem.Core
{
    /// <summary>
    /// 模组服务接口
    /// 所有可注册的服务都必须实现此接口
    /// </summary>
    public interface IModService
    {
        /// <summary>
        /// 服务的唯一标识符
        /// </summary>
        string ServiceId { get; }
        
        /// <summary>
        /// 提供服务的模组ID
        /// </summary>
        string ProviderId { get; }
        
        /// <summary>
        /// 服务版本号
        /// </summary>
        string Version { get; }
    }
} 