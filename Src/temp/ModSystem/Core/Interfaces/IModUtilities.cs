using System.Collections;

namespace ModSystem.Core
{
    /// <summary>
    /// 模组工具接口
    /// 提供辅助功能
    /// </summary>
    public interface IModUtilities
    {
        /// <summary>
        /// 启动协程
        /// </summary>
        /// <param name="enumerator">协程枚举器</param>
        /// <returns>协程句柄</returns>
        object StartCoroutine(System.Collections.IEnumerator enumerator);
        
        /// <summary>
        /// 停止协程
        /// </summary>
        /// <param name="coroutine">协程句柄</param>
        void StopCoroutine(object coroutine);
    }
} 