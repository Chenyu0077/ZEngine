//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hotfix.FuncModule
{
    /// <summary>
    /// 多次添加同一种Buff的效果类型
    /// </summary>
    public enum BuffMutipleAddType
    {
        RestTime,               //重置Buff时间
        AddLayer,               //增加Buff层数
        AddLayerAndResetTime,   //增加Buff层数并且重置Buff时间
        AddCount,               //同种Buff同时存在多个，互不影响
    }

    /// <summary>
    /// Buff的类型标签
    /// </summary>
    public enum BuffTag
    {
        None = 0,
        Buff = 1 << 0,
        Debuff = 1 << 1,
        Control = 1 << 2,
        Passive = 1 << 3,
        Trigger = 1 << 4,
    }

    /// <summary>
    /// Buff类型
    /// </summary>
    public enum BuffType
    {
        Default,
        Heal,
        Damage,
        Roll,
    }
}
