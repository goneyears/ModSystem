using System;
using System.Collections.Generic;
using System.Text;

namespace ModSystem.Core
{
    /// <summary>
    /// 模组基类 - 提供默认实现
    /// </summary>
    public abstract class ModBase : IMod
    {
        protected IModContext Context { get; private set; }

        public virtual void OnInitialize(IModContext context)
        {
            Context = context;
            Context.Log($"Mod {Context.ModId} initialized");
        }

        public virtual void OnEnable()
        {
            Context.Log($"Mod {Context.ModId} enabled");
        }

        public virtual void OnDisable()
        {
            Context.Log($"Mod {Context.ModId} disabled");
        }

        public virtual void OnDestroy()
        {
            Context.Log($"Mod {Context.ModId} destroyed");
        }
    }
}
