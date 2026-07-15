//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace Hotfix.FuncModule
{
    /// <summary>
    /// Buff拥有者继承的接口
    /// </summary>
    public interface IBuffHandler
    {
        /// <summary>
        /// 角色所拥有的所有Buff
        /// </summary>
        List<BuffBase> Buffs { get; }

        /// <summary>
        /// 添加Buff
        /// </summary>
        /// <param name="buff_id"></param>
        /// <param name="caster"></param>
        void AddBuff(int buff_id, GameObject caster);

        /// <summary>
        /// 移除Buff
        /// </summary>
        /// <param name="buff_id"></param>
        void RemoveBuff(int buff_id);
    }
}