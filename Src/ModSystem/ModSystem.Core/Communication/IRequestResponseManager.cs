using System;
using System.Threading.Tasks;

namespace ModSystem.Core
{
    /// <summary>
    /// 请求响应管理器接口
    /// </summary>
    public interface IRequestResponseManager
    {
        /// <summary>
        /// 发送请求并等待响应
        /// </summary>
        /// <typeparam name="TRequest">请求类型</typeparam>
        /// <typeparam name="TResponse">响应类型</typeparam>
        /// <param name="request">请求对象</param>
        /// <param name="timeout">超时时间</param>
        /// <returns>响应对象</returns>
        Task<TResponse> SendRequestAsync<TRequest, TResponse>(
            TRequest request, 
            TimeSpan? timeout = null) 
            where TRequest : ModRequest 
            where TResponse : ModResponse;
    }
} 