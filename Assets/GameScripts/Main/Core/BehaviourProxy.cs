//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using System.Collections;
using UnityEngine;

namespace Main.Core
{
    /// <summary>
    /// 通用 Mono 驱动壳。
    ///
    /// 用途：热更工程里的纯 C# 逻辑类若需要 GameObject 层面的能力（协程、独立生命周期、
    /// Unity 帧回调），不必自己继承 MonoBehaviour（热更 Mono 有序列化/预制体绑定限制），
    /// 而是运行时申请一个 BehaviourProxy，把回调委托进来。
    ///
    /// 注意：仅当需要 <b>协程</b> 或 <b>独立于全局的 GameObject 生命周期</b> 时才用它。
    /// 若只需要每帧 Update，直接实现 <see cref="IHotUpdateModule"/> 由 HotUpdateMain 统一驱动即可，
    /// 无需 Proxy。
    ///
    /// 使用示例：
    /// <code>
    ///   var proxy = BehaviourProxy.Create(nameof(MyMgr), onUpdate: Tick, onDestroy: Cleanup);
    ///   proxy.StartCoroutine(SomeRoutine());
    ///   // 不再需要时：
    ///   proxy.Dispose();
    /// </code>
    /// </summary>
    public sealed class BehaviourProxy : MonoBehaviour
    {
        private Action _onUpdate;
        private Action _onLateUpdate;
        private Action _onFixedUpdate;
        private Action _onDestroy;

        /// <summary>
        /// 创建一个独立的驱动壳。GameObject 默认 DontDestroyOnLoad，跨场景常驻。
        /// </summary>
        /// <param name="name">GameObject 名称，便于在 Hierarchy 中定位</param>
        /// <param name="onUpdate">每帧 Update 回调</param>
        /// <param name="onLateUpdate">每帧 LateUpdate 回调</param>
        /// <param name="onFixedUpdate">每帧 FixedUpdate 回调</param>
        /// <param name="onDestroy">销毁时回调（Dispose 或 GameObject 被销毁时触发）</param>
        /// <param name="dontDestroyOnLoad">是否跨场景常驻，默认 true</param>
        public static BehaviourProxy Create(
            string name,
            Action onUpdate = null,
            Action onLateUpdate = null,
            Action onFixedUpdate = null,
            Action onDestroy = null,
            bool dontDestroyOnLoad = true)
        {
            var go = new GameObject(string.IsNullOrEmpty(name) ? nameof(BehaviourProxy) : name);
            if (dontDestroyOnLoad)
                DontDestroyOnLoad(go);

            var proxy = go.AddComponent<BehaviourProxy>();
            proxy._onUpdate = onUpdate;
            proxy._onLateUpdate = onLateUpdate;
            proxy._onFixedUpdate = onFixedUpdate;
            proxy._onDestroy = onDestroy;
            return proxy;
        }

        private void Update()      => _onUpdate?.Invoke();
        private void LateUpdate()  => _onLateUpdate?.Invoke();
        private void FixedUpdate() => _onFixedUpdate?.Invoke();

        private void OnDestroy()
        {
            _onDestroy?.Invoke();
            _onUpdate = _onLateUpdate = _onFixedUpdate = _onDestroy = null;
        }

        /// <summary>
        /// 暴露协程能力给热更逻辑类（MonoBehaviour.StartCoroutine 是 protected 的实例方法，
        /// 这里用 new 提升为 public 便于外部调用）。
        /// </summary>
        public new Coroutine StartCoroutine(IEnumerator routine) => base.StartCoroutine(routine);

        /// <summary>停止指定协程。</summary>
        public new void StopCoroutine(Coroutine routine)
        {
            if (routine != null) base.StopCoroutine(routine);
        }

        /// <summary>停止该壳上的所有协程。</summary>
        public new void StopAllCoroutines() => base.StopAllCoroutines();

        /// <summary>主动销毁该驱动壳及其 GameObject。</summary>
        public void Dispose()
        {
            if (this != null && gameObject != null)
                Destroy(gameObject);
        }
    }
}
