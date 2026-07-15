//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Hotfix.Main.Logic;
using UnityEngine;
using ZEngine.Manager.Log;

namespace Hotfix.Core
{
    public class HotUpdateMain
    {
        private readonly List<IHotUpdateModule> _modules = new List<IHotUpdateModule>();

        public HotUpdateMain()
        {
            Assembly assembly = typeof(HotUpdateMain).Assembly;
            var types = assembly.GetTypes()
                .Where(t => typeof(IHotUpdateModule).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null);
            _modules.AddRange(types.Select(t => (IHotUpdateModule)Activator.CreateInstance(t))
                .OrderBy(m => m.Priority));
        }

        public void Update()
        {
            foreach (var module in _modules)
            {
                module.Update();
            }
        }

        public void LateUpdate()
        {
            foreach (var module in _modules)
            {
                module.LateUpdate();
            }
        }

        public void FixedUpdate()
        {
            foreach (var module in _modules)
            {
                module.FixedUpdate();
            }
        }

        public void Destroy()
        {
            for (int i = _modules.Count - 1; i >= 0; i--)
                _modules[i].Destroy();
            _modules.Clear();
        }

        public void Run()
        {
            RunAsync().Forget();
        }

        private async UniTaskVoid RunAsync()
        {
            foreach (var module in _modules)
                module.Initialize();

            LogManager.Instance.Info("Game Start...");
            ModuleMgr.I.Initialize();
            ModuleMgr.I.Fsm.Run("InitNode");
        }
    }
}
