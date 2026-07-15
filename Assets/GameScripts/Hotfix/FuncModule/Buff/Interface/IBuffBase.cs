//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;

namespace Hotfix.FuncModule
{
    /// <summary>
    /// Buff的接口，定义Buff的生命周期
    /// </summary>
    public interface IBuffBase
    {
        /// <summary>
        /// Buff生效前执行（即使Buff不作用于对象也会执行）
        /// </summary>
        void OnBuffAwake();

        /// <summary>
        /// Buff开始生效时
        /// </summary>
        void OnBuffStart();

        /// <summary>
        /// Buff周期性计时更新
        /// </summary>
        void OnBuffUpdate();

        /// <summary>
        /// Buff移除时
        /// </summary>
        void OnBuffRemove();

        /// <summary>
        /// Buff销毁时
        /// </summary>
        void OnBuffDestroy();

        /// <summary>
        /// 开启周期性效果
        /// </summary>
        void StartBuffTickEffect(float interval);

        /// <summary>
        /// 停止周期性效果
        /// </summary>
        void StopBuffTickEffect();

        /// <summary>
        /// 重置Buff
        /// </summary>
        void Reset();

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="caster"></param>
        void Initialize(IBuffHandler owner, GameObject caster, BuffData buffData);

        /// <summary>
        /// Buff的周期性效果
        /// </summary>
        void OnBuffTickEffect();

        /// <summary>
        /// 设置Buff是否生效（Buff不生效时，计时器也是停止的）
        /// </summary>
        /// <param name="isEffective"></param>
        void SetEffective(bool isEffective);

        /// <summary>
        /// 修改Layer的层级（默认是层级+1）
        /// </summary>
        /// <param name="addCount"></param>
        void ModifyLayer(int addCount = 1);

        /// <summary>
        /// 检查Buff是否有该标签
        /// </summary>
        /// <param name="checkedTag"></param>
        bool HasTag(BuffTag checkedTag);

        /// <summary>
        /// 是否能添加Buff
        /// </summary>
        /// <returns></returns>
        bool IsCanAddBuff(IBuffHandler buffHandler, BuffBase buff);
    }
}
