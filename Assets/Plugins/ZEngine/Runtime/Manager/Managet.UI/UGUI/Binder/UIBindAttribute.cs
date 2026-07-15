//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;

namespace ZEngine.Manager.UI.UGUI
{
    /// <summary>
    /// 字段自动绑定特性。
    /// 标记在 UBaseView 子类的字段上，框架在 BuildChildCache 末尾由 UIBinder 自动
    /// 按路径查找子节点上的组件并注入。路径格式同 Transform.Find（如 "Header/NameText"）。
    /// 同时支持原生 UGUI 类型（Button / TextMeshProUGUI / Image / GameObject …）
    /// 与 Components 层包装类型（UIButton / UIText / UIImage …），业务可渐进式迁移。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public class UIBindAttribute : Attribute
    {
        /// <summary>
        /// 相对 View 根节点的路径（格式同 Transform.Find）
        /// </summary>
        public string Path { get; }

        public UIBindAttribute(string path)
        {
            Path = path;
        }
    }
}
