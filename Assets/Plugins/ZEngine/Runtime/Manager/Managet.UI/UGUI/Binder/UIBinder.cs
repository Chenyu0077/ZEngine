//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ZEngine.Manager.Log;

namespace ZEngine.Manager.UI.UGUI
{
    /// <summary>
    /// 自动绑定器：扫描 UBaseView 子类带 [UIBind] 的字段，复用 View 已构建的
    /// _childComponents 缓存按路径注入组件，零额外 Transform 遍历。
    /// 字段数组按类型缓存，反射开销分摊到首次打开。
    /// </summary>
    public static class UIBinder
    {
        /// <summary>一条待绑定字段的全量信息：字段 + 路径 + 是否 GameObject 类型。在缓存时一次性算好。</summary>
        private readonly struct BindEntry
        {
            public readonly FieldInfo Field;
            public readonly string Path;
            public readonly bool IsGameObject;

            public BindEntry(FieldInfo field, string path, bool isGameObject)
            {
                Field = field;
                Path = path;
                IsGameObject = isGameObject;
            }
        }

        // 按 View 类型缓存其带 [UIBind] 的字段绑定信息，只反射一次；
        // 仅缓存带 [UIBind] 的字段（已过滤），避免 Bind 时遍历无关字段、重复 GetCustomAttribute。
        private static readonly Dictionary<Type, BindEntry[]> _bindCache = new Dictionary<Type, BindEntry[]>();

        // 逐层声明字段（DeclaredOnly），用于沿基类链收集 [UIBind] 字段
        private const BindingFlags DECL_FLAGS =
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        /// <summary>
        /// 对 View 上所有 [UIBind] 字段执行自动注入。在 UBaseView.BuildChildCache 末尾调用，
        /// 保证 Initialize / OnComplete 时字段已就绪。对象池复用时 BuildChildCache 会重新执行，幂等。
        /// </summary>
        public static void Bind(ZEngine.Manager.UI.UBaseView view)
        {
            if (view == null)
                return;

            Type type = view.GetType();
            BindEntry[] entries = GetOrCreateBindings(type);

            for (int i = 0; i < entries.Length; i++)
            {
                ref var entry = ref entries[i];
                object value = entry.IsGameObject
                    ? view.GetChildGO(entry.Path)
                    : view.GetChild(entry.Field.FieldType, entry.Path);

                if (value != null)
                {
                    entry.Field.SetValue(view, value);
                }
                else
                {
                    // 路径未命中仅告警，不抛异常，避免单节点缺失拖垮整窗
                    LogManager.Instance.Warning(
                        $"[UIBinder] View[{type.Name}] 路径[{entry.Path}] 未找到 {entry.Field.FieldType.Name}，字段[{entry.Field.Name}] 留空");
                }
            }
        }

        /// <summary>
        /// 沿基类链从叶子类型向基类逐层（DeclaredOnly）收集带 [UIBind] 的字段并预取路径/类型分类，缓存复用。
        /// 用 DeclaredOnly 逐层收集，是为了让 UIWindow/UIPopup 等基类声明的 [UIBind] 脚手架字段
        /// （protected/private）在子类实例上也能被发现并绑定（GetFields 默认不返回继承的非 public 字段）。
        /// </summary>
        private static BindEntry[] GetOrCreateBindings(Type type)
        {
            if (!_bindCache.TryGetValue(type, out var entries))
            {
                var list = new List<BindEntry>();
                for (var t = type; t != null && t != typeof(object); t = t.BaseType)
                {
                    var fields = t.GetFields(DECL_FLAGS);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var attr = (UIBindAttribute)fields[i].GetCustomAttribute(typeof(UIBindAttribute), false);
                        if (attr == null)
                            continue;
                        list.Add(new BindEntry(fields[i], attr.Path, fields[i].FieldType == typeof(GameObject)));
                    }
                }
                entries = list.ToArray();
                _bindCache.Add(type, entries);
            }
            return entries;
        }
    }
}
