//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;

namespace ZEngine.Manager.UI.UGUI.Components
{
    /// <summary>
    /// UI 组件基类（MonoBehaviour，挂载在预制体子节点上）。
    /// 组件化范式：每个可复用 UI 控件包装一个原生 UGUI 组件，
    /// 由 [UIBind] 或 GetChild<T> 取用，业务层只与包装组件交互。
    /// </summary>
    public abstract class UIComponentBase : MonoBehaviour
    {
        /// <summary>由宿主 View 在绑定后调用，做一次性初始化</summary>
        public virtual void OnInit() { }

        /// <summary>由宿主 View 释放时调用，清理监听/句柄</summary>
        public virtual void OnRelease() { }
    }
}
