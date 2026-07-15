//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;

namespace Hotfix.FuncModule
{
    /// <summary>
    /// 默认Buff
    /// </summary>
    [BuffType(BuffType.Default)]
    public class DefaultBuff : BuffBase
    {
        public DefaultBuff(IBuffHandler owner, GameObject caster, BuffData buffData) : base(owner, caster, buffData)
        {
        }

        public override void OnBuffTickEffect()
        {
            
        }
    }
}
