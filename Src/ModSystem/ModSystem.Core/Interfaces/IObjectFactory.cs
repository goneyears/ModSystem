using System.Threading.Tasks;

namespace ModSystem.Core
{
    /// <summary>
    /// 平台无关的对象工厂接口
    /// 用于创建游戏对象
    /// </summary>
    public interface IObjectFactory
    {
        /// <summary>
        /// 从定义文件创建对象
        /// </summary>
        /// <param name="definitionPath">对象定义文件路径</param>
        /// <returns>创建的游戏对象</returns>
        Task<IGameObject> CreateObjectAsync(string definitionPath);
        
        /// <summary>
        /// 从对象定义创建对象
        /// </summary>
        /// <param name="definition">对象定义</param>
        /// <returns>创建的游戏对象</returns>
        Task<IGameObject> CreateObjectFromDefinitionAsync(ObjectDefinition definition);
    }
} 