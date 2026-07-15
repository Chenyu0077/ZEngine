using System.Collections;
using DG.Tweening;
using FairyGUI;
using UnityEngine;

/// <summary>
/// FairyGUI GObject 的 DOTween 扩展
///
/// 基础用法：
///   obj.DOAlpha(1f, 0.3f).SetEase(Ease.OutQuad);
///   obj.DOMove(new Vector2(200, 100), 0.5f).SetEase(Ease.OutExpo);
///
/// 链式设置（Set 前缀）：
///   obj.DOAlpha(1f, 0.3f).SetDelay(0.5f).SetLoops(2, LoopType.Yoyo).SetAutoKill(false);
///
/// 回调（On 前缀）：
///   obj.DOAlpha(0f, 0.3f).OnComplete(() => obj.visible = false);
///
/// Sequence 组合：
///   DOTween.Sequence()
///          .Append(obj.DOAlpha(1f, 0.3f))
///          .Join(obj.DOScale(1f, 0.3f))
///          .AppendInterval(0.5f)
///          .Append(obj.DOMove(new Vector2(100, 0), 0.4f));
///
/// 控制：
///   obj.DOPause(); obj.DOPlay(); obj.DORestart(); obj.DOKill();
///
/// 协程等待：
///   yield return obj.DOAlpha(1f, 0.5f).WaitForCompletion();
/// </summary>
public static class FGUITween
{
    #region 基础 Tweener

    #region Alpha

    /// <summary>透明度动画 (0~1)</summary>
    public static Tweener DOAlpha(this GObject target, float to, float duration)
        => DOTween.To(() => target.alpha, v => target.alpha = v, to, duration).SetTarget(target);

    #endregion

    #region 位置

    /// <summary>移动到目标位置</summary>
    public static Tweener DOMove(this GObject target, Vector2 to, float duration)
        => DOTween.To(() => target.xy, v => target.xy = v, to, duration).SetTarget(target);

    /// <summary>仅移动 X 轴</summary>
    public static Tweener DOMoveX(this GObject target, float to, float duration)
        => DOTween.To(() => target.x, v => target.x = v, to, duration).SetTarget(target);

    /// <summary>仅移动 Y 轴</summary>
    public static Tweener DOMoveY(this GObject target, float to, float duration)
        => DOTween.To(() => target.y, v => target.y = v, to, duration).SetTarget(target);

    #endregion

    #region 缩放

    /// <summary>缩放到目标值（XY 独立）</summary>
    public static Tweener DOScale(this GObject target, Vector2 to, float duration)
        => DOTween.To(() => target.scale, v => target.scale = v, to, duration).SetTarget(target);

    /// <summary>缩放到目标值（XY 等比）</summary>
    public static Tweener DOScale(this GObject target, float to, float duration)
        => DOTween.To(() => target.scale, v => target.scale = v,
                      new Vector2(to, to), duration).SetTarget(target);

    /// <summary>仅缩放 X 轴</summary>
    public static Tweener DOScaleX(this GObject target, float to, float duration)
        => DOTween.To(() => target.scale.x, v => target.scale = new Vector2(v, target.scale.y),
                      to, duration).SetTarget(target);

    /// <summary>仅缩放 Y 轴</summary>
    public static Tweener DOScaleY(this GObject target, float to, float duration)
        => DOTween.To(() => target.scale.y, v => target.scale = new Vector2(target.scale.x, v),
                      to, duration).SetTarget(target);

    #endregion

    #region 旋转

    /// <summary>旋转到目标角度（度）</summary>
    public static Tweener DORotation(this GObject target, float to, float duration)
        => DOTween.To(() => target.rotation, v => target.rotation = v, to, duration).SetTarget(target);

    #endregion

    #region 尺寸

    /// <summary>改变组件尺寸</summary>
    public static Tweener DOSize(this GObject target, Vector2 to, float duration)
        => DOTween.To(() => new Vector2(target.width, target.height),
                      v => target.SetSize(v.x, v.y), to, duration).SetTarget(target);

    /// <summary>仅改变宽度</summary>
    public static Tweener DOWidth(this GObject target, float to, float duration)
        => DOTween.To(() => target.width, v => target.width = v, to, duration).SetTarget(target);

    /// <summary>仅改变高度</summary>
    public static Tweener DOHeight(this GObject target, float to, float duration)
        => DOTween.To(() => target.height, v => target.height = v, to, duration).SetTarget(target);

    #endregion

    #region 颜色

    /// <summary>GImage 颜色动画</summary>
    public static Tweener DOColor(this GImage target, Color to, float duration)
        => DOTween.To(() => target.color, v => target.color = v, to, duration).SetTarget(target);

    /// <summary>GImage 透明度动画（仅 Alpha 通道）</summary>
    public static Tweener DOFade(this GImage target, float to, float duration)
    {
        Color c = target.color;
        return DOTween.To(() => target.color.a, v => target.color = new Color(c.r, c.g, c.b, v),
                          to, duration).SetTarget(target);
    }

    /// <summary>GGraph 颜色动画</summary>
    public static Tweener DOColor(this GGraph target, Color to, float duration)
        => DOTween.To(() => target.color, v => target.color = v, to, duration).SetTarget(target);

    /// <summary>GTextField 文字颜色动画</summary>
    public static Tweener DOColor(this GTextField target, Color to, float duration)
        => DOTween.To(() => target.color, v => target.color = v, to, duration).SetTarget(target);

    /// <summary>GTextField 文字透明度动画</summary>
    public static Tweener DOFade(this GTextField target, float to, float duration)
    {
        Color c = target.color;
        return DOTween.To(() => target.color.a, v => target.color = new Color(c.r, c.g, c.b, v),
                          to, duration).SetTarget(target);
    }

    #endregion

    #endregion

    #region 控制方法

    /// <summary>暂停该 GObject 上的所有动画</summary>
    public static void DOPause(this GObject target) => DOTween.Pause(target);

    /// <summary>播放该 GObject 上的所有动画</summary>
    public static void DOPlay(this GObject target) => DOTween.Play(target);

    /// <summary>向前播放</summary>
    public static void DOPlayForward(this GObject target) => DOTween.PlayForward(target);

    /// <summary>倒放</summary>
    public static void DOPlayBackwards(this GObject target) => DOTween.PlayBackwards(target);

    /// <summary>重新播放（从头开始）</summary>
    public static void DORestart(this GObject target) => DOTween.Restart(target);

    /// <summary>停止并销毁该 GObject 上所有动画；complete=true 时跳到终态</summary>
    public static void DOKill(this GObject target, bool complete = false)
        => DOTween.Kill(target, complete);

    #endregion

    #region 协程等待

    /// <summary>
    /// 等待某个 Tweener/Sequence 完成后继续协程
    /// 用法：yield return StartCoroutine(FGUITween.WaitForTween(myTween));
    /// </summary>
    public static IEnumerator WaitForTween(Tween tween)
    {
        yield return tween.WaitForCompletion();
    }

    #endregion

    #region 快捷组合动效

    /// <summary>
    /// 淡入：alpha 0→1，可选从偏移方向滑入
    /// slideOffset 示例：new Vector2(0, 50) = 从下方 50px 滑入
    /// </summary>
    public static Sequence DOFadeIn(this GObject target, float duration,
                                    Vector2? slideOffset = null, Ease ease = Ease.OutExpo)
    {
        target.alpha = 0f;
        Sequence seq = DOTween.Sequence().SetTarget(target);

        if (slideOffset.HasValue)
        {
            Vector2 origin = target.xy;
            target.xy = origin + slideOffset.Value;
            seq.Join(target.DOMove(origin, duration).SetEase(ease));
        }

        seq.Join(target.DOAlpha(1f, duration).SetEase(Ease.OutQuad));
        return seq;
    }

    /// <summary>
    /// 淡出：alpha 1→0，可选向偏移方向滑出
    /// slideOffset 示例：new Vector2(0, -50) = 向上方 50px 滑出
    /// </summary>
    public static Sequence DOFadeOut(this GObject target, float duration,
                                     Vector2? slideOffset = null, Ease ease = Ease.InExpo,
                                     bool hideOnComplete = true)
    {
        Sequence seq = DOTween.Sequence().SetTarget(target);

        if (slideOffset.HasValue)
            seq.Join(target.DOMove(target.xy + slideOffset.Value, duration).SetEase(ease));

        seq.Join(target.DOAlpha(0f, duration).SetEase(Ease.InQuad));

        if (hideOnComplete)
            seq.OnComplete(() => target.visible = false);

        return seq;
    }

    /// <summary>
    /// 弹性缩放出现（常用于弹窗、提示框入场）
    /// scale 0.5→1，alpha 0→1
    /// </summary>
    public static Sequence DOPopIn(this GObject target, float duration = 0.35f)
    {
        target.alpha = 0f;
        target.scale = new Vector2(0.5f, 0.5f);
        return DOTween.Sequence().SetTarget(target)
               .Join(target.DOScale(1f, duration).SetEase(Ease.OutBack))
               .Join(target.DOAlpha(1f, duration * 0.6f).SetEase(Ease.OutQuad));
    }

    /// <summary>
    /// 弹性缩放消失，scale 1→0，alpha 1→0
    /// </summary>
    public static Sequence DOPopOut(this GObject target, float duration = 0.25f,
                                    bool hideOnComplete = true)
    {
        Sequence seq = DOTween.Sequence().SetTarget(target)
               .Join(target.DOScale(0f, duration).SetEase(Ease.InBack))
               .Join(target.DOAlpha(0f, duration).SetEase(Ease.InQuad));

        if (hideOnComplete)
            seq.OnComplete(() => target.visible = false);

        return seq;
    }

    /// <summary>
    /// 水平震动（错误提示、受击反馈）
    /// strength：震动幅度（px），vibrato：震动次数
    /// </summary>
    public static Sequence DOShake(this GObject target, float duration = 0.4f,
                                   float strength = 10f, int vibrato = 10)
    {
        float originX = target.x;
        Sequence seq = DOTween.Sequence().SetTarget(target);
        float stepDuration = duration / (vibrato + 1);

        for (int i = 0; i < vibrato; i++)
        {
            float offset = (i % 2 == 0 ? strength : -strength) * (1f - (float)i / vibrato);
            seq.Append(target.DOMoveX(originX + offset, stepDuration).SetEase(Ease.Linear));
        }

        seq.Append(target.DOMoveX(originX, stepDuration).SetEase(Ease.OutQuad));
        return seq;
    }

    /// <summary>
    /// 呼吸效果（循环缩放），适合强调某个元素
    /// 调用后会一直循环，需要手动 DOKill() 停止
    /// </summary>
    public static Tweener DOBreath(this GObject target, float minScale = 0.95f,
                                   float maxScale = 1.05f, float duration = 1f)
    {
        target.scale = new Vector2(minScale, minScale);
        return target.DOScale(maxScale, duration)
                     .SetEase(Ease.InOutSine)
                     .SetLoops(-1, LoopType.Yoyo)
                     .SetTarget(target);
    }

    /// <summary>
    /// Punch 冲击缩放（按钮点击反馈）
    /// 先缩小到 punchScale，再弹回 1
    /// </summary>
    public static Sequence DOPunch(this GObject target, float punchScale = 0.85f,
                                   float duration = 0.3f)
    {
        float half = duration * 0.4f;
        return DOTween.Sequence().SetTarget(target)
               .Append(target.DOScale(punchScale, half).SetEase(Ease.OutQuad))
               .Append(target.DOScale(1f, duration - half).SetEase(Ease.OutBack));
    }

    /// <summary>
    /// 沿路径移动（折线）
    /// waypoints：相对于父容器的坐标列表
    /// </summary>
    public static Sequence DOPath(this GObject target, Vector2[] waypoints, float duration,
                                  Ease ease = Ease.Linear)
    {
        if (waypoints == null || waypoints.Length == 0) return DOTween.Sequence();

        float stepDuration = duration / waypoints.Length;
        Sequence seq = DOTween.Sequence().SetTarget(target);

        foreach (Vector2 point in waypoints)
            seq.Append(target.DOMove(point, stepDuration).SetEase(ease));

        return seq;
    }

    #endregion
}
