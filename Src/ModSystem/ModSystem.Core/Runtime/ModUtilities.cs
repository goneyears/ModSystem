using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 模组工具实现
    /// </summary>
    internal class ModUtilities : IModUtilities
    {
        public object StartCoroutine(System.Collections.IEnumerator enumerator)
        {
            // 需要在Unity层实现
            throw new NotImplementedException("Coroutines require Unity implementation");
        }
        
        public void StopCoroutine(object coroutine)
        {
            // 需要在Unity层实现
            throw new NotImplementedException("Coroutines require Unity implementation");
        }
    }
} 