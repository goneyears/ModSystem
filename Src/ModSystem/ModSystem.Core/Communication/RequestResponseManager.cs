using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace ModSystem.Core
{
    /// <summary>
    /// 请求响应管理器实现
    /// </summary>
    public class RequestResponseManager : IRequestResponseManager, IDisposable
    {
        private readonly IEventBus eventBus;
        private readonly Dictionary<string, PendingRequest> pendingRequests;
        private readonly Timer cleanupTimer;
        private readonly object lockObject = new object();
        
        /// <summary>
        /// 待处理请求信息
        /// </summary>
        private class PendingRequest
        {
            public TaskCompletionSource<ModResponse> CompletionSource { get; set; }
            public Type ResponseType { get; set; }
            public DateTime CreatedAt { get; set; }
            public CancellationTokenSource CancellationTokenSource { get; set; }
        }
        
        /// <summary>
        /// 创建请求响应管理器
        /// </summary>
        /// <param name="eventBus">事件总线</param>
        public RequestResponseManager(IEventBus eventBus)
        {
            this.eventBus = eventBus;
            this.pendingRequests = new Dictionary<string, PendingRequest>();
            
            // 定期清理超时请求
            cleanupTimer = new Timer(CleanupTimeoutRequests, null, 
                TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }
        
        /// <summary>
        /// 发送请求并等待响应
        /// </summary>
        public async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
            TRequest request, 
            TimeSpan? timeout = null) 
            where TRequest : ModRequest 
            where TResponse : ModResponse
        {
            var actualTimeout = timeout ?? TimeSpan.FromSeconds(30);
            var cts = new CancellationTokenSource(actualTimeout);
            var tcs = new TaskCompletionSource<ModResponse>();
            
            // 注册取消回调
            cts.Token.Register(() =>
            {
                tcs.TrySetCanceled();
                CleanupRequest(request.RequestId);
            });
            
            lock (lockObject)
            {
                pendingRequests[request.RequestId] = new PendingRequest
                {
                    CompletionSource = tcs,
                    ResponseType = typeof(TResponse),
                    CreatedAt = DateTime.Now,
                    CancellationTokenSource = cts
                };
            }
            
            // 订阅响应事件
            Action<TResponse> responseHandler = null;
            responseHandler = (response) =>
            {
                if (response.RequestId == request.RequestId)
                {
                    lock (lockObject)
                    {
                        if (pendingRequests.TryGetValue(request.RequestId, out var pending))
                        {
                            pending.CompletionSource.TrySetResult(response);
                            CleanupRequest(request.RequestId);
                        }
                    }
                    eventBus.Unsubscribe(responseHandler);
                }
            };
            
            eventBus.Subscribe(responseHandler);
            eventBus.Publish(request);
            
            try
            {
                var result = await tcs.Task;
                return (TResponse)result;
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException($"Request {request.RequestId} timed out after {actualTimeout}");
            }
        }
        
        /// <summary>
        /// 清理请求
        /// </summary>
        private void CleanupRequest(string requestId)
        {
            lock (lockObject)
            {
                if (pendingRequests.TryGetValue(requestId, out var pending))
                {
                    pending.CancellationTokenSource?.Dispose();
                    pendingRequests.Remove(requestId);
                }
            }
        }
        
        /// <summary>
        /// 清理超时请求
        /// </summary>
        private void CleanupTimeoutRequests(object state)
        {
            var now = DateTime.Now;
            List<string> timeoutRequests;
            
            lock (lockObject)
            {
                timeoutRequests = pendingRequests
                    .Where(kvp => (now - kvp.Value.CreatedAt) > TimeSpan.FromMinutes(5))
                    .Select(kvp => kvp.Key)
                    .ToList();
            }
            
            foreach (var requestId in timeoutRequests)
            {
                lock (lockObject)
                {
                    if (pendingRequests.TryGetValue(requestId, out var pending))
                    {
                        pending.CompletionSource.TrySetException(
                            new TimeoutException("Request timed out during cleanup")
                        );
                        CleanupRequest(requestId);
                    }
                }
            }
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            cleanupTimer?.Dispose();
            
            // 取消所有待处理请求
            lock (lockObject)
            {
                foreach (var requestId in pendingRequests.Keys.ToList())
                {
                    CleanupRequest(requestId);
                }
            }
        }
    }
} 