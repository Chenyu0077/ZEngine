//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

namespace ZEngine.Manager.UI.UGUI.Animation
{
    /// <summary>
    /// UI 动画工具：基于 DOTween 的常用 UGUI 动画预设。
    ///
    /// 设计参考 FGUITween：基础 Tween（DO 前缀，返回 Tweener/Sequence）+ 快捷组合动效（DO 前缀）+
    /// 控制方法（Kill/Play/Pause）。
    ///
    /// 重要：ZEngine asmdef 不引用 Assembly-CSharp，拿不到 DOTween UI 模块扩展
    /// （CanvasGroup.DOFade / RectTransform.DOAnchorPos 等）。故本工具一律用核心 DOTween.To(getter,setter,...)
    /// 重写，仅依赖 DOTween.dll 核心 API + Transform.DOScale/DORotate（核心已含）。
    ///
    /// 基础用法：
    ///   UIAnimation.DOAlpha(cg, 1f, 0.3f).SetEase(Ease.OutQuad);
    ///   UIAnimation.DOAnchoredPos(rt, target, 0.5f).SetEase(Ease.OutExpo);
    ///
    /// 组合动效（返回 Sequence）：
    ///   UIAnimation.DOPopIn(rt);
    ///   UIAnimation.DOFadeIn(cg, 0.3f, slideOffset: new Vector2(0, 50));
    ///
    /// 控制：
    ///   UIAnimation.Kill(target);          // 按 SetTarget 的 target kill 该对象所有 tween
    ///   UIAnimation.Kill(ref tween);       // kill 指定 tween 引用并置 null
    /// </summary>
    public static class UIAnimation
    {
        public const float DefaultDuration = 0.25f;

        #region 基础 Tween —— 透明度

        /// <summary>CanvasGroup 透明度动画 (0~1)</summary>
        public static Tweener DOAlpha(CanvasGroup target, float to, float duration = DefaultDuration)
            => DOTween.To(() => target.alpha, v => target.alpha = v, to, duration).SetTarget(target);

        /// <summary>Graphic(Image/Text 等) 透明度动画（改 color.a）</summary>
        public static Tweener DOFade(Graphic target, float to, float duration = DefaultDuration)
            => DOTween.To(() => target.color, v => target.color = v,
                          new Color(target.color.r, target.color.g, target.color.b, to), duration).SetTarget(target);

        /// <summary>TextMeshProUGUI 文字透明度动画</summary>
        public static Tweener DOFade(TextMeshProUGUI target, float to, float duration = DefaultDuration)
            => DOTween.To(() => target.color, v => target.color = v,
                          new Color(target.color.r, target.color.g, target.color.b, to), duration).SetTarget(target);

        #endregion

        #region 基础 Tween —— 位置 (RectTransform anchoredPosition)

        public static Tweener DOAnchoredPos(RectTransform target, Vector2 to, float duration = DefaultDuration)
            => DOTween.To(() => target.anchoredPosition, v => target.anchoredPosition = v, to, duration).SetTarget(target);

        public static Tweener DOAnchoredPosX(RectTransform target, float to, float duration = DefaultDuration)
            => DOTween.To(() => target.anchoredPosition.x,
                          v => target.anchoredPosition = new Vector2(v, target.anchoredPosition.y),
                          to, duration).SetTarget(target);

        public static Tweener DOAnchoredPosY(RectTransform target, float to, float duration = DefaultDuration)
            => DOTween.To(() => target.anchoredPosition.y,
                          v => target.anchoredPosition = new Vector2(target.anchoredPosition.x, v),
                          to, duration).SetTarget(target);

        #endregion

        #region 基础 Tween —— 缩放 / 旋转 / 尺寸

        /// <summary>缩放到目标（等比）—— Transform.DOScale 为 DOTween 核心 API</summary>
        public static Tweener DOScale(Transform target, float to, float duration = DefaultDuration)
            => target.DOScale(to, duration).SetTarget(target);

        public static Tweener DOScaleX(Transform target, float to, float duration = DefaultDuration)
            => DOTween.To(() => target.localScale.x,
                          v => target.localScale = new Vector3(v, target.localScale.y, target.localScale.z),
                          to, duration).SetTarget(target);

        public static Tweener DOScaleY(Transform target, float to, float duration = DefaultDuration)
            => DOTween.To(() => target.localScale.y,
                          v => target.localScale = new Vector3(target.localScale.x, v, target.localScale.z),
                          to, duration).SetTarget(target);

        /// <summary>旋转到目标欧拉角（Z 轴，常用于图标转动）</summary>
        public static Tweener DORotationZ(Transform target, float to, float duration = DefaultDuration)
            => DOTween.To(() => target.localEulerAngles.z,
                          v => target.localRotation = Quaternion.Euler(0, 0, v), to, duration).SetTarget(target);

        /// <summary>RectTransform 尺寸动画</summary>
        public static Tweener DOSizeDelta(RectTransform target, Vector2 to, float duration = DefaultDuration)
            => DOTween.To(() => target.sizeDelta, v => target.sizeDelta = v, to, duration).SetTarget(target);

        #endregion

        #region 基础 Tween —— 颜色

        public static Tweener DOColor(Graphic target, Color to, float duration = DefaultDuration)
            => DOTween.To(() => target.color, v => target.color = v, to, duration).SetTarget(target);

        public static Tweener DOColor(TextMeshProUGUI target, Color to, float duration = DefaultDuration)
            => DOTween.To(() => target.color, v => target.color = v, to, duration).SetTarget(target);

        #endregion

        #region 控制方法

        /// <summary>kill 某对象(SetTarget 绑定)上的所有 tween。complete=true 跳到终态。</summary>
        public static void Kill(Object target, bool complete = false) => DOTween.Kill(target, complete);

        /// <summary>安全 kill 指定 tween 引用并置 null（已 null/完成则跳过）。</summary>
        public static void Kill(ref Tween tween)
        {
            if (tween != null && tween.IsActive())
                tween.Kill();
            tween = null;
        }

        public static void Play(Object target) => DOTween.Play(target);
        public static void Pause(Object target) => DOTween.Pause(target);
        public static void Restart(Object target) => DOTween.Restart(target);

        #endregion

        #region 快捷组合动效 —— 弹窗入场/退场

        /// <summary>弹出缩放打开：0.85 -> 1，带回弹。返回 Tween 供 UIWindow.OnOpenAnimation 使用。</summary>
        public static Tween PopOpen(Transform target, float duration = DefaultDuration)
        {
            if (target == null) return null;
            target.localScale = Vector3.one * 0.85f;
            return target.DOScale(Vector3.one, duration).SetEase(Ease.OutBack).SetTarget(target);
        }

        /// <summary>收缩关闭：1 -> 0.85，内收。</summary>
        public static Tween PopClose(Transform target, float duration = DefaultDuration)
        {
            if (target == null) return null;
            return target.DOScale(Vector3.one * 0.85f, duration).SetEase(Ease.InBack).SetTarget(target);
        }

        /// <summary>
        /// 淡入：CanvasGroup alpha 0→1，可选从偏移方向滑入。
        /// slideOffset 示例：new Vector2(0, 50) = 从下方 50px 滑入。
        /// </summary>
        public static Sequence FadeIn(CanvasGroup target, float duration = DefaultDuration,
                                      Vector2? slideOffset = null, Ease ease = Ease.OutExpo)
        {
            if (target == null) return null;
            target.alpha = 0f;
            target.blocksRaycasts = false;
            Sequence seq = DOTween.Sequence().SetTarget(target);
            RectTransform rt = target.GetComponent<RectTransform>();
            if (slideOffset.HasValue && rt != null)
            {
                Vector2 origin = rt.anchoredPosition;
                rt.anchoredPosition = origin + slideOffset.Value;
                seq.Join(DOAnchoredPos(rt, origin, duration).SetEase(ease));
            }
            seq.Join(DOAlpha(target, 1f, duration).SetEase(Ease.OutQuad))
               .OnComplete(() => target.blocksRaycasts = true);
            return seq;
        }

        /// <summary>淡出：CanvasGroup alpha -> 0，期间关闭交互。</summary>
        public static Sequence FadeOut(CanvasGroup target, float duration = DefaultDuration,
                                       Vector2? slideOffset = null, Ease ease = Ease.InExpo)
        {
            if (target == null) return null;
            target.blocksRaycasts = false;
            Sequence seq = DOTween.Sequence().SetTarget(target);
            RectTransform rt = target.GetComponent<RectTransform>();
            if (slideOffset.HasValue && rt != null)
                seq.Join(DOAnchoredPos(rt, rt.anchoredPosition + slideOffset.Value, duration).SetEase(ease));
            seq.Join(DOAlpha(target, 0f, duration).SetEase(Ease.InQuad));
            return seq;
        }

        /// <summary>弹性缩放出现：scale 0.5→1 + alpha 0→1（需 CanvasGroup 控 alpha，否则仅缩放）。</summary>
        public static Sequence PopIn(RectTransform target, CanvasGroup cg = null, float duration = 0.35f)
        {
            if (target == null) return null;
            target.localScale = Vector3.one * 0.5f;
            Sequence seq = DOTween.Sequence().SetTarget(target);
            seq.Join(DOScale(target, 1f, duration).SetEase(Ease.OutBack));
            if (cg != null)
            {
                cg.alpha = 0f;
                seq.Join(DOAlpha(cg, 1f, duration * 0.6f).SetEase(Ease.OutQuad));
            }
            return seq;
        }

        /// <summary>弹性缩放消失：scale 1→0.85 + alpha 1→0（若有 CanvasGroup）。</summary>
        public static Sequence PopOut(RectTransform target, CanvasGroup cg = null, float duration = 0.25f)
        {
            if (target == null) return null;
            Sequence seq = DOTween.Sequence().SetTarget(target);
            seq.Join(DOScale(target, 0.85f, duration).SetEase(Ease.InBack));
            if (cg != null)
                seq.Join(DOAlpha(cg, 0f, duration).SetEase(Ease.InQuad));
            return seq;
        }

        #endregion

        #region 快捷组合动效 —— 反馈/强调

        /// <summary>水平震动（错误提示、受击反馈）。strength 幅度(px)，vibrato 震动次数。</summary>
        public static Sequence Shake(RectTransform target, float duration = 0.4f,
                                     float strength = 10f, int vibrato = 10)
        {
            if (target == null) return null;
            float originX = target.anchoredPosition.x;
            Sequence seq = DOTween.Sequence().SetTarget(target);
            float stepDuration = duration / (vibrato + 1);
            for (int i = 0; i < vibrato; i++)
            {
                float offset = (i % 2 == 0 ? strength : -strength) * (1f - (float)i / vibrato);
                seq.Append(DOAnchoredPosX(target, originX + offset, stepDuration).SetEase(Ease.Linear));
            }
            seq.Append(DOAnchoredPosX(target, originX, stepDuration).SetEase(Ease.OutQuad));
            return seq;
        }

        /// <summary>呼吸效果（循环缩放），适合强调元素。需手动 Kill 停止。</summary>
        public static Tweener Breath(Transform target, float minScale = 0.95f,
                                     float maxScale = 1.05f, float duration = 1f)
        {
            if (target == null) return null;
            target.localScale = Vector3.one * minScale;
            return target.DOScale(maxScale, duration)
                         .SetEase(Ease.InOutSine)
                         .SetLoops(-1, LoopType.Yoyo)
                         .SetTarget(target);
        }

        /// <summary>Punch 冲击缩放（按钮点击反馈）：先缩到 punchScale 再弹回 1。</summary>
        public static Sequence Punch(Transform target, float punchScale = 0.85f, float duration = 0.3f)
        {
            if (target == null) return null;
            float half = duration * 0.4f;
            Sequence seq = DOTween.Sequence().SetTarget(target);
            seq.Append(target.DOScale(punchScale, half).SetEase(Ease.OutQuad));
            seq.Append(target.DOScale(1f, duration - half).SetEase(Ease.OutBack));
            return seq;
        }

        /// <summary>沿折线路径移动（waypoints 为 anchoredPosition 目标列表）。</summary>
        public static Sequence Path(RectTransform target, Vector2[] waypoints, float duration,
                                    Ease ease = Ease.Linear)
        {
            if (target == null || waypoints == null || waypoints.Length == 0)
                return DOTween.Sequence();
            float stepDuration = duration / waypoints.Length;
            Sequence seq = DOTween.Sequence().SetTarget(target);
            foreach (var point in waypoints)
                seq.Append(DOAnchoredPos(target, point, stepDuration).SetEase(ease));
            return seq;
        }

        #endregion
    }
}
