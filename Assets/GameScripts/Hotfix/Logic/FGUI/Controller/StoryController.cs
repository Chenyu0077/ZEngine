//------------------------------
// ZEngine
// 作者:
//------------------------------

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FairyGUI;
using Hotfix.UI.Generate.Main;
using UnityEngine;
using ZEngine.Manager.Log;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    /// <summary>
    /// 故事背景面板控制器。
    /// 对应 StoryView.xml 字段：
    ///   storyScroll — ScrollText1 组件，m_content 为打字机文本
    ///   cursor      — 光标闪烁符（GTextField）
    ///   skipBtn     — 全屏透明点击层（跳过）
    /// </summary>
    public class StoryController : BaseController
    {
        private UIStoryView     _compt;
        private StoryModel     _storyModel;

        private UIScrollText1  _storyScroll;
        private GGraph         _skipBtn;

        private CancellationTokenSource _cts;
        private bool _finished;

        private string _typedText = "";
        private bool   _cursorOn = true;

        public override void Initialize()
        {
            base.Initialize();
            _compt      = _view.GetView() as UIStoryView;
            _storyModel = _view.Data as StoryModel;
            if (_compt == null) return;

            _storyScroll = _compt.m_storyScroll as UIScrollText1;
            _skipBtn     = _compt.m_skipBtn as GGraph;
            
            // 覆盖 ScrollText1 默认样式，与改版前保持一致
            if (_storyScroll?.m_content != null)
            {
                var fmt = _storyScroll.m_content.textFormat;
                fmt.size    = 28;
                fmt.color   = new Color(1f, 1f, 1f, 1f);
                fmt.lineSpacing = 12;
                _storyScroll.m_content.textFormat = fmt;
                _storyScroll.m_content.text = "";
            }

            _cts = new CancellationTokenSource();

            _compt.GetTransition("show")?.Play();

            if (_skipBtn != null)
                _skipBtn.onClick.Add(OnSkip);

            RunStoryAsync(_cts.Token).Forget();
        }

        public override void OnRelease()
        {
            if (_skipBtn != null)
                _skipBtn.onClick.Remove(OnSkip);

            _cts?.Cancel();
            _cts?.Dispose();
            _cts        = null;
            _compt      = null;
            _storyModel = null;
            base.OnRelease();
        }

        // ── 跳过点击 ──
        private void OnSkip(EventContext ctx) => TryFinish(instant: true);

        // ── 故事主流程 ──
        private async UniTaskVoid RunStoryAsync(CancellationToken ct)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1.25), cancellationToken: ct);
                await TypewriterAsync(ct);
                if (ct.IsCancellationRequested) return;

                // 打字完毕：隐藏光标
                _cursorOn = false;
                RefreshText();

                // 等待自动关闭
                float delay = _storyModel?.AutoCloseDelay ?? 4f;
                if (delay >= 0)
                    await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: ct);

                if (!ct.IsCancellationRequested)
                    TryFinish(instant: false);
            }
            catch (OperationCanceledException) { /* 跳过时取消，正常路径 */ }
        }

        // ── 打字机 ──
        private async UniTask TypewriterAsync(CancellationToken ct)
        {
            if (_storyScroll == null || _storyModel == null || _storyScroll.m_content == null) return;

            string text = _storyModel.StoryText;
            _typedText = "";
            _cursorOn  = true;
            RefreshText();

            int charDelay = Mathf.RoundToInt(_storyModel.TypeInterval * 1000f);

            BlinkCursorAsync(ct).Forget();

            foreach (char c in text)
            {
                ct.ThrowIfCancellationRequested();
                _typedText += c;
                RefreshText();
                _storyScroll.scrollPane?.ScrollBottom(false);
                await UniTask.Delay(charDelay, cancellationToken: ct);
            }
        }

        // 重新渲染当前正文 + 末尾光标
        private void RefreshText()
        {
            if (_storyScroll?.m_content == null) return;
            _storyScroll.m_content.text = _typedText + (_cursorOn ? "|" : "");
        }

        // ── 光标闪烁 ──
        private async UniTaskVoid BlinkCursorAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await UniTask.Delay(500, cancellationToken: ct);
                    _cursorOn = !_cursorOn;
                    RefreshText();
                }
            }
            catch (OperationCanceledException) { }
        }

        // ── 结束故事 ──
        private void TryFinish(bool instant)
        {
            if (_finished) return;
            _finished = true;

            // 取消打字机和计时
            _cts?.Cancel();

            if (instant)
            {
                CloseAndCallback();
            }
            else
            {
                var hide = _compt?.GetTransition("hide");
                if (hide != null)
                    hide.Play(CloseAndCallback);
                else
                    CloseAndCallback();
            }
        }

        private void CloseAndCallback()
        {
            var cb = _storyModel?.OnFinished;
            UIManager.Instance.CloseView<StoryView>();
            cb?.Invoke();
        }
    }
}
