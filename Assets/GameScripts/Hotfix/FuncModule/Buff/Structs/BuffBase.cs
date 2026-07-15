//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using UnityEngine;
using ZEngine.Manager.Log;

namespace Hotfix.FuncModule
{
    /// <summary>
    /// Buff的基类，实现Buff的生命周期
    /// </summary>
    public abstract class BuffBase : IBuffBase
    {
        /// <summary>
        /// 实例化Buff对象的唯一ID
        /// </summary>
        private string _guid = Guid.NewGuid().ToString();
        public string GUID
        {
            get { return _guid; }
        }

        /// <summary>
        /// Buff的拥有者
        /// </summary>
        public IBuffHandler Owner { get; private set; }

        /// <summary>
        /// Buff的产生者（可能为null）
        /// </summary>
        public GameObject Caster { get; private set; }

        /// <summary>
        /// Buff配置数据
        /// </summary>
        public BuffData BuffData { get; private set; }


        #region BuffData中的一些固定信息
        public int Buff_Id => BuffData?.buff_id ?? -1;
        public string Buff_Name => BuffData?.buff_name ?? string.Empty;
        public string Buff_Icon => BuffData?.icon ?? string.Empty;
        public string Description => BuffData?.description ?? string.Empty;
        public int Buff_Priority => BuffData?.priority ?? -1;
        public bool CanAddLayer => BuffData?.canAddLayer ?? false;
        public BuffType BuffType => BuffData?.buffType ?? BuffType.Default;
        public BuffType[] MutexBuffs => BuffData?.mutexBuffs ?? null;
        #endregion


        #region Buff生命周期相关参数
        private float _duration;
        private float _elapsedTime;
        private int _layer;
        #endregion


        #region Buff周期性参数相关
        private bool _startTickTime;//是否开启周期性计时
        private float _tickInterval;//周期性触发间隔
        private float _tickElapsedTime;//周期性进行的时间（用来实现计时）
        #endregion

        private bool _isEffective;

        /// <summary>
        /// Buff的总持续时间（一个周期的时长）
        /// </summary>
        public float Duration
        {
            get
            {
                return _duration;
            }

            private set
            {
                _duration = value;
            }
        }

        /// <summary>
        /// Buff已经进行的时间
        /// </summary>
        public float ElapsedTime
        {
            get
            {
                return _elapsedTime;
            }

            private set
            {
                _elapsedTime = value;
            }
        }

        /// <summary>
        /// Buff进行的进度
        /// </summary>
        public float Progress
        {
            get
            {
                return _elapsedTime / _duration;
            }
        }

        /// <summary>
        /// Buff当前层数
        /// </summary>
        public int Layer
        {
            get
            {
                return _layer;
            }
            private set
            {
                _layer = value;
            }
        }

        public BuffMutipleAddType BuffAddType => BuffData?.mutipleAddType ?? BuffMutipleAddType.RestTime;

        /// <summary>
        /// Buff类型标签
        /// </summary>
        public BuffTag Tag => BuffData?.tag ?? BuffTag.None;

        /// <summary>
        /// Buff是否是永久的
        /// </summary>
        public bool IsPermanent
        {
            get
            {
                if (_duration <= 0)
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Buff是否有效
        /// </summary>
        public bool IsEffective => _isEffective;


        /// <summary>
        /// Buff被添加时的回调
        /// </summary>
        public Action<IBuffHandler, BuffBase> OnBuffAdded;

        /// <summary>
        /// Buff被移除时的回调
        /// </summary>
        public Action<IBuffHandler, BuffBase> OnBuffRemoved;


        public BuffBase(IBuffHandler owner, GameObject caster, BuffData buffData)
        {
            Initialize(owner, caster, buffData);
        }

        #region 生命周期函数
        public void Initialize(IBuffHandler owner, GameObject caster, BuffData buffData)
        {
            this.Owner = owner;
            this.Caster = caster;
            this.BuffData = buffData;
        }

        public virtual void OnBuffAwake()
        {
            if (BuffData == null)
            {
                LogManager.Instance.Error($"[{GetType().Name}] BuffData is null in OnBuffAwake.");
                return;
            }

            _duration = BuffData.duration;
            _tickInterval = BuffData.tickInterval;
            _elapsedTime = 0;
            _layer = BuffData.layer;
            _isEffective = true;
        }

        public virtual void OnBuffStart() 
        {
            OnBuffAdded?.Invoke(Owner, this);
            StartBuffTickEffect(_tickInterval);
        }

        public virtual void OnBuffUpdate()
        {
            if (!IsEffective)
                return;

            // 在 FixedUpdate 中 Time.deltaTime 等于 fixedDeltaTime
            float timePassed = Time.deltaTime;

            // 控制持续时间（永久 buff 不计时）
            if (_duration > 0)
            {
                _elapsedTime += timePassed;
                while (_elapsedTime >= _duration)
                {
                    _elapsedTime -= _duration; // 保留溢出部分，避免计时精度丢失
                    _layer--;
                    if (_layer <= 0)
                    {
                        _elapsedTime = 0;
                        _isEffective = false;
                        _startTickTime = false;
                        OnBuffRemove();
                        OnBuffDestroy();
                        return; // 已销毁，不再处理周期触发
                    }
                }
            }

            // 控制周期触发（interval <= 0 时不触发，避免死循环）
            if (!_startTickTime || _tickInterval <= 0f)
                return;

            _tickElapsedTime += timePassed;
            while (_tickElapsedTime >= _tickInterval)
            {
                _tickElapsedTime -= _tickInterval;
                OnBuffTickEffect();
            }
        }

        public virtual void OnBuffRemove() 
        {
            StopBuffTickEffect();
            OnBuffRemoved?.Invoke(Owner, this);
        }

        public virtual void OnBuffDestroy()
        {
            _layer = 0;
            _isEffective = false;
            _startTickTime = false;
            // 清理回调委托链，避免持有者因未取消订阅而造成内存泄漏
            OnBuffAdded = null;
            OnBuffRemoved = null;
        }

        /// <summary>
        /// 重置Buff的持续时间计时（不改变当前层数）
        /// </summary>
        public void Reset()
        {
            _elapsedTime = 0;
            _tickElapsedTime = 0;
        }

        public void StartBuffTickEffect(float interval)
        {
            _startTickTime = true;
            _tickElapsedTime = 0;
            _tickInterval = interval;
        }

        public void StopBuffTickEffect()
        {
            _startTickTime = false;
        }
        #endregion


        public abstract void OnBuffTickEffect();

        public void SetEffective(bool isEffective)
        {
            _isEffective = isEffective;
        }

        public void ModifyLayer(int addCount = 1)
        {
            if (!CanAddLayer)
            {
                LogManager.Instance.Info($"Buff [{Buff_Name}] 不支持层数叠加");
                return;
            }

            _layer += addCount;
            if (_layer < 0)
                _layer = 0;
        }

        public bool HasTag(BuffTag checkedTag)
        {
            return (Tag & checkedTag) != 0;
        }

        public virtual bool IsCanAddBuff(IBuffHandler buffHandler, BuffBase buff)
        {
            return true;
        }
    }
}
