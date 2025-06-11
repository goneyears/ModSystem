namespace ModSystem.Core
{
    /// <summary>
    /// 模组API实现
    /// </summary>
    internal class ModAPI : IModAPI
    {
        public IRequestResponseManager RequestResponse { get; set; }
        public IObjectFactory ObjectFactory { get; set; }
        public IModUtilities Utilities { get; set; }
    }
} 