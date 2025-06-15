namespace ModSystem.Core.Interfaces
{
    /// <summary>
    /// 模组行为接口
    /// </summary>
    public interface IModBehaviour
    {
        string ModId { get; }
        void Initialize(Runtime.ModContext context);
        void Shutdown();
    }
}