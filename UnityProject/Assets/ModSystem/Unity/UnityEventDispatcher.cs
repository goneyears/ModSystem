using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace ModSystem.Unity.Events
{
    /// <summary>
    /// Unity主线程调度器
    /// </summary>
    public class UnityEventDispatcher : MonoBehaviour
    {
        private readonly ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();
        private static UnityEventDispatcher _instance;

        public static UnityEventDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[EventDispatcher]");
                    _instance = go.AddComponent<UnityEventDispatcher>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        void Update()
        {
            while (_mainThreadQueue.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }

        public void RunOnMainThread(Action action)
        {
            if (action != null)
                _mainThreadQueue.Enqueue(action);
        }
    }
}