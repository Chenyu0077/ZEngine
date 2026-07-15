//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;

namespace Hotfix.FuncModule
{
    /// <summary>
    /// 用于自定义Buff类型的特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class BuffTypeAttribute : Attribute
    {
        public BuffType BuffType { get; }

        public BuffTypeAttribute(BuffType buffType)
        {
            BuffType = buffType;
        }
    }
}
