//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using Cysharp.Threading.Tasks;
using Main.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using ZEngine.Core;
using ZEngine.Manager.Resource;

namespace Hotfix.FuncModule
{
    /// <summary>
    /// Buff管理器
    /// </summary>
    public class BuffManager : BehaviourSingleton<BuffManager>
    {

        private string buffConfigPath = HotfixAssetPaths.SOBuffPath;

        private BuffConfig buffConfig;

        private Dictionary<int, BuffData> buffDatasDic = new Dictionary<int, BuffData>();

        private List<IBuffHandler> buffHandlers = new List<IBuffHandler>();


        private void Start()
        {
            //检测依赖模块
            if (ZEngineMain.Contains(typeof(ResourceManager)) == false)
                throw new Exception($"{nameof(BuffManager)}依赖于{nameof(ResourceManager)}");

            RunInitForSO().Forget(Debug.LogException);
        }

        private async UniTask RunInitForSO()
        {
            var handle = await ResourceManager.Instance.LoadAssetAsync<BuffConfig>(buffConfigPath);
            buffConfig = handle.AssetObject as BuffConfig;
            foreach (var item in buffConfig.buffDatas)
            {
                if (!buffDatasDic.TryGetValue(item.buff_id, out BuffData buffData))
                    buffDatasDic[item.buff_id] = item;
                else
                    throw new System.Exception($"出现Buff的ID相同的状况，{item.buff_name}: {item.buff_id}");
            }
        }

        private void FixedUpdate()
        {
            for (int h = 0; h < buffHandlers.Count; h++)
            {
                var handler = buffHandlers[h];
                var buffs = handler.Buffs;
                bool changed = false;

                // 反向遍历，便于在遍历中原地移除已失效的Buff
                for (int i = buffs.Count - 1; i >= 0; i--)
                {
                    var buff = buffs[i];
                    buff.OnBuffUpdate();

                    // OnBuffUpdate 内部自然过期时已调用 OnBuffRemove/OnBuffDestroy，
                    // 这里只负责把它从列表中移除
                    if (!buff.IsEffective)
                    {
                        buffs.RemoveAt(i);
                        changed = true;
                    }
                }

                if (changed)
                {
                    // 刷新角色属性
                    ChaAttrRecheck(handler);
                }

                // 该handler已无Buff，移除对其的引用
                if (buffs.Count == 0)
                {
                    buffHandlers.RemoveAt(h);
                    h--;
                }
            }
        }

        protected override void OnDestroy()
        {
            foreach (var handler in buffHandlers)
            {
                var buffs = handler.Buffs;
                for (int i = buffs.Count - 1; i >= 0; i--)
                {
                    var buff = buffs[i];
                    buff.OnBuffRemove();
                    buff.OnBuffDestroy();
                }
                buffs.Clear();
            }
            buffHandlers.Clear();

            base.OnDestroy();
        }


        /// <summary>
        /// 添加Buff
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="buff_Id"></param>
        /// <param name="caster"></param>
        /// <returns></returns>
        public BuffBase AddBuff(IBuffHandler handler, int buff_Id, GameObject caster = null)
        {
            if (handler == null) return null;

            BuffData buffData = GetBuffData(buff_Id);
            if (buffData == null)
            {
                Debug.LogError($"未查询到BuffData数据, id: {buff_Id}");
                return null;
            }

            // 调用CanAddBuff进行条件判断（互斥判断）
            if (!CanAddBuff(handler, buffData))
            {
                Debug.LogWarning($"无法添加Buff(互斥): {buffData.buff_name}");
                return null;
            }

            if (!buffHandlers.Contains(handler))
                buffHandlers.Add(handler);

            BuffBase oldBuff = handler.Buffs.Find(x => x.Buff_Id == buff_Id);
            if (oldBuff == null)
            {
                // 不存在同ID Buff，创建新实例
                BuffBase buff = BuffFactory.CreateBuff(handler, caster, buffData);
                if (!buff.IsCanAddBuff(handler, buff))
                {
                    Debug.LogWarning($"无法添加Buff: {buffData.buff_name}");
                    return null;
                }

                handler.Buffs.Add(buff);
                buff.OnBuffAwake();
                buff.OnBuffStart();
                SortBuffs(handler);
                ChaAttrRecheck(handler);
                return buff;
            }

            // 已存在同ID Buff，根据多次添加的处理类型决定行为
            BuffBase result = oldBuff;
            bool needNewInstance = false;
            switch (oldBuff.BuffAddType)
            {
                case BuffMutipleAddType.RestTime:
                    oldBuff.Reset();
                    break;
                case BuffMutipleAddType.AddLayer:
                    oldBuff.ModifyLayer(1);
                    break;
                case BuffMutipleAddType.AddLayerAndResetTime:
                    // 在当前层数基础上+1并重置时间（避免层数丢失）
                    oldBuff.Reset();
                    oldBuff.ModifyLayer(1);
                    break;
                case BuffMutipleAddType.AddCount:
                    // 同种Buff共存多个，需要创建新实例
                    needNewInstance = true;
                    break;
                default:
                    break;
            }

            if (needNewInstance)
            {
                BuffBase buff = BuffFactory.CreateBuff(handler, caster, buffData);
                if (!buff.IsCanAddBuff(handler, buff))
                {
                    Debug.LogWarning($"无法添加Buff: {buffData.buff_name}");
                    return null;
                }
                handler.Buffs.Add(buff);
                buff.OnBuffAwake();
                buff.OnBuffStart();
                result = buff;
            }

            SortBuffs(handler);
            ChaAttrRecheck(handler);
            return result;
        }


        /// <summary>
        /// 移除指定ID的所有Buff
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="buff_id"></param>
        public void RemoveBuff(IBuffHandler handler, int buff_id)
        {
            if (handler == null) return;

            var buffs = handler.Buffs;
            bool changed = false;
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                var buff = buffs[i];
                if (buff.Buff_Id == buff_id)
                {
                    buffs.RemoveAt(i);
                    buff.SetEffective(false);
                    buff.OnBuffRemove();
                    buff.OnBuffDestroy();
                    changed = true;
                }
            }

            if (changed)
                ChaAttrRecheck(handler);

            if (buffs.Count == 0)
                buffHandlers.Remove(handler);
        }

        /// <summary>
        /// 移除所有拥有指定标签的Buff（用于净化等场景）
        /// </summary>
        public void RemoveBuffByTag(IBuffHandler handler, BuffTag tag)
        {
            if (handler == null) return;

            var buffs = handler.Buffs;
            bool changed = false;
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                var buff = buffs[i];
                if (buff.HasTag(tag))
                {
                    buffs.RemoveAt(i);
                    buff.SetEffective(false);
                    buff.OnBuffRemove();
                    buff.OnBuffDestroy();
                    changed = true;
                }
            }

            if (changed)
                ChaAttrRecheck(handler);

            if (buffs.Count == 0)
                buffHandlers.Remove(handler);
        }

        /// <summary>
        /// 移除指定类型的所有Buff
        /// </summary>
        public void RemoveBuffByType(IBuffHandler handler, BuffType buffType)
        {
            if (handler == null) return;

            var buffs = handler.Buffs;
            bool changed = false;
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                var buff = buffs[i];
                if (buff.BuffType == buffType)
                {
                    buffs.RemoveAt(i);
                    buff.SetEffective(false);
                    buff.OnBuffRemove();
                    buff.OnBuffDestroy();
                    changed = true;
                }
            }

            if (changed)
                ChaAttrRecheck(handler);

            if (buffs.Count == 0)
                buffHandlers.Remove(handler);
        }

        /// <summary>
        /// 是否拥有指定ID的Buff
        /// </summary>
        public bool HasBuff(IBuffHandler handler, int buff_id)
        {
            if (handler == null) return false;
            return handler.Buffs.Exists(x => x.Buff_Id == buff_id);
        }

        /// <summary>
        /// 是否拥有指定标签的Buff
        /// </summary>
        public bool HasBuffWithTag(IBuffHandler handler, BuffTag tag)
        {
            if (handler == null) return false;
            return handler.Buffs.Exists(x => x.HasTag(tag));
        }


        /// <summary>
        /// 获取Buff的配置信息
        /// </summary>
        /// <param name="buff_id"></param>
        /// <returns></returns>
        public BuffData GetBuffData(int buff_id)
        {
            if (buffDatasDic.TryGetValue(buff_id, out var buffData))
                return buffData;
            Debug.LogWarning($"未找到 ID 为 {buff_id} 的Buff数据");
            return null;
        }


        /// <summary>
        /// 是否可以添加Buff（互斥判断：待添加Buff与已存在Buff互相检查互斥列表）
        /// </summary>
        private bool CanAddBuff(IBuffHandler buffHandler, BuffData buffData)
        {
            BuffType newType = buffData.buffType;
            BuffType[] newMutex = buffData.mutexBuffs;

            foreach (var buff in buffHandler.Buffs)
            {
                // 待添加Buff与已存在Buff互斥
                if (newMutex != null)
                {
                    foreach (var mutexType in newMutex)
                    {
                        if (mutexType == buff.BuffType)
                            return false;
                    }
                }

                // 已存在Buff声明与待添加Buff互斥
                var existMutex = buff.MutexBuffs;
                if (existMutex != null)
                {
                    foreach (var mutexType in existMutex)
                    {
                        if (mutexType == newType)
                            return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 按优先级对handler的Buff列表排序（优先级小的在前）
        /// </summary>
        private void SortBuffs(IBuffHandler handler)
        {
            handler.Buffs.Sort((a, b) => a.Buff_Priority.CompareTo(b.Buff_Priority));
        }


        /// <summary>
        /// 刷新角色属性。
        /// 当 handler 为 EntityBase（塔/纸人运行时实体）时，回调其 RecalculateAttributes()
        /// 重算配置基值 + 等级缩放 + Buff 修饰后的最终属性。
        /// </summary>
        private void ChaAttrRecheck(IBuffHandler handler)
        {
            if (handler is EntityBase entity)
                entity.RecalculateAttributes();
        }
    }
}
