using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZEngine.Core
{
    /// <summary>
    /// The root entry point of the ZEngine framework.
    /// Manages the lifecycle of all sub-systems.
    /// </summary>
    public class GameRoot : MonoSingleton<GameRoot>
    {
        private readonly List<IModule> _modules = new List<IModule>();
        private bool _initialized = false;

        protected override void OnInit()
        {
            if (_initialized) return;
            _initialized = true;
            Debug.Log("[GameRoot] ZEngine framework initialized.");
        }

        /// <summary>
        /// Register a module with the framework.
        /// </summary>
        public void RegisterModule(IModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            if (_modules.Contains(module)) return;
            _modules.Add(module);
            module.OnCreate();
        }

        /// <summary>
        /// Unregister and dispose a module from the framework.
        /// </summary>
        public void UnregisterModule(IModule module)
        {
            if (module == null) return;
            if (_modules.Remove(module))
            {
                module.OnDispose();
            }
        }

        /// <summary>
        /// Get a registered module by type.
        /// </summary>
        public T GetModule<T>() where T : class, IModule
        {
            foreach (var module in _modules)
            {
                if (module is T typedModule)
                    return typedModule;
            }
            return null;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = 0; i < _modules.Count; i++)
            {
                _modules[i].OnUpdate(deltaTime);
            }
        }

        private void FixedUpdate()
        {
            float fixedDeltaTime = Time.fixedDeltaTime;
            for (int i = 0; i < _modules.Count; i++)
            {
                _modules[i].OnFixedUpdate(fixedDeltaTime);
            }
        }

        protected override void OnDestroy()
        {
            for (int i = _modules.Count - 1; i >= 0; i--)
            {
                _modules[i].OnDispose();
            }
            _modules.Clear();
            base.OnDestroy();
        }
    }
}
