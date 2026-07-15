using System;
using UnityEngine;
using DG.Tweening;

namespace Main.FuncModule.Camera2D
{
    /// <summary>
    /// 剧情相机操作基类，由 Camera2DController.EnqueueCinematic() 入队后依次执行。
    /// </summary>
    public abstract class CinematicOp
    {
        public abstract void Execute(Camera2DController ctrl, Action onComplete);
    }

    /// <summary>平滑移动相机到指定世界位置</summary>
    public class CinematicMove : CinematicOp
    {
        private readonly Vector2 destination;
        private readonly float duration;
        private readonly Ease ease;

        public CinematicMove(Vector2 destination, float duration, Ease ease = Ease.InOutSine)
        {
            this.destination = destination;
            this.duration = duration;
            this.ease = ease;
        }

        public override void Execute(Camera2DController ctrl, Action onComplete)
        {
            Vector3 target = new Vector3(destination.x, destination.y, ctrl.transform.position.z);
            ctrl.transform.DOMove(target, duration)
                .SetEase(ease)
                .OnComplete(() =>
                {
                    // 同步 targetPos，避免 SmoothDamp 在剧情结束后拉回旧位置
                    ctrl.SnapTo(destination);
                    onComplete?.Invoke();
                });
        }
    }

    /// <summary>平滑缩放正交相机尺寸</summary>
    public class CinematicZoom : CinematicOp
    {
        private readonly float targetSize;
        private readonly float duration;
        private readonly Ease ease;

        public CinematicZoom(float targetSize, float duration, Ease ease = Ease.InOutSine)
        {
            this.targetSize = targetSize;
            this.duration = duration;
            this.ease = ease;
        }

        public override void Execute(Camera2DController ctrl, Action onComplete)
        {
            // orthographicSize 必须 > 0，否则渲染崩溃
            float safeSize = Mathf.Max(0.01f, targetSize);
            ctrl.ActiveCamera.DOOrthoSize(safeSize, duration)
                .SetEase(ease)
                .OnComplete(() =>
                {
                    ctrl.SnapZoom(safeSize);
                    onComplete?.Invoke();
                });
        }
    }

    /// <summary>同时平移并缩放（并行执行，等两者都完成再触发回调）</summary>
    public class CinematicMoveAndZoom : CinematicOp
    {
        private readonly Vector2 destination;
        private readonly float targetSize;
        private readonly float duration;
        private readonly Ease ease;

        public CinematicMoveAndZoom(Vector2 destination, float targetSize, float duration, Ease ease = Ease.InOutSine)
        {
            this.destination = destination;
            this.targetSize = targetSize;
            this.duration = duration;
            this.ease = ease;
        }

        public override void Execute(Camera2DController ctrl, Action onComplete)
        {
            float safeSize = Mathf.Max(0.01f, targetSize);

            int remaining = 2;
            void Done()
            {
                if (--remaining == 0)
                {
                    ctrl.SnapTo(destination);
                    ctrl.SnapZoom(safeSize);
                    onComplete?.Invoke();
                }
            }

            Vector3 target = new Vector3(destination.x, destination.y, ctrl.transform.position.z);
            ctrl.transform.DOMove(target, duration).SetEase(ease).OnComplete(Done);
            ctrl.ActiveCamera.DOOrthoSize(safeSize, duration).SetEase(ease).OnComplete(Done);
        }
    }

    /// <summary>等待指定时长后继续下一个操作</summary>
    public class CinematicWait : CinematicOp
    {
        private readonly float seconds;

        public CinematicWait(float seconds) => this.seconds = seconds;

        public override void Execute(Camera2DController ctrl, Action onComplete)
        {
            // SetTarget 使此 tween 归属 ctrl.transform，ClearCinematic 的 DOKill 可正确终止它
            DOVirtual.DelayedCall(seconds, () => onComplete?.Invoke())
                     .SetTarget(ctrl.transform);
        }
    }

    /// <summary>执行自定义委托（用于剧情中插入游戏逻辑）</summary>
    public class CinematicCallback : CinematicOp
    {
        private readonly Action callback;

        public CinematicCallback(Action callback) => this.callback = callback;

        public override void Execute(Camera2DController ctrl, Action onComplete)
        {
            callback?.Invoke();
            onComplete?.Invoke();
        }
    }
}
