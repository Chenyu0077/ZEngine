//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;

namespace Hotfix.FuncModule
{
    /// <summary>
    /// Buff配置数据
    /// </summary>
    [CreateAssetMenu(menuName = "CreateCustomSO/BuffData")]
    public class BuffData : ScriptableObject
    {
        [Tooltip("唯一ID")]
        public int buff_id;
        [Tooltip("名称")]
        public string buff_name;
        [Tooltip("Buff类型")]
        public BuffType buffType;
        [Tooltip("图片地址")]
        public string icon;
        [Tooltip("描述")]
        public string description;
        [Tooltip("持续时间(如果小于等于0，则Buff长期存在)")]
        public float duration;
        [Tooltip("间隔触发时间")]
        public float tickInterval;
        [Tooltip("优先级，优先级越低的buff越后面执行，这是一个非常重要的属性")]
        public int priority;
        [Tooltip("是否支持叠加层数")]
        public bool canAddLayer;
        [Tooltip("层数")]
        public int layer;
        [Tooltip("多次添加同一种Buff的效果类型")]
        public BuffMutipleAddType mutipleAddType;
        [Tooltip("Buff的类型标签")]
        public BuffTag tag;
        [Tooltip("Buff的互斥Buffs，即有Buffs时，该Buff不能被添加")]
        public BuffType[] mutexBuffs;

        public static BuffData DefaultData => new BuffData()
        {
            buff_id = -1,
            buff_name = "default",
            buffType = BuffType.Default,
        };
    }
}
