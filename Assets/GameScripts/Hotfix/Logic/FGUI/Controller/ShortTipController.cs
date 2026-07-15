//------------------------------
// ZEngine
// 作者:
//------------------------------

using DG.Tweening;
using Hotfix.UI.Generate.Main;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class ShortTipController : BaseController
    {
        private const float SlideTime   = 0.4f;
        private const float DisplayTime = 5f;
        private const float TopPadding  = 10f;

        private UIShortTipView _compt;
        private new ShortTipModel _model;
        private Sequence _seq;

        public override void Initialize()
        {
            base.Initialize();
            _compt = _view.GetView() as UIShortTipView;
            _model = base._model as ShortTipModel;
            if (_compt == null) return;

            _compt.m_content.text = _model?.Content ?? "";

            float showY = _compt.height + TopPadding;
            float hideY = -_compt.height;
            _compt.y = hideY;

            // 滑入 → 等待 → 滑出 → 关闭
            _seq = DOTween.Sequence()
                .Append(_compt.DOMoveY(showY, SlideTime).SetEase(Ease.OutCubic))
                .AppendInterval(DisplayTime)
                .Append(_compt.DOMoveY(hideY, SlideTime).SetEase(Ease.InCubic))
                .OnComplete(() => UIManager.Instance.CloseView<ShortTipView>());
        }

        public override void OnRelease()
        {
            _seq?.Kill();
            _seq = null;
            _compt = null;
            _model = null;
            base.OnRelease();
        }
    }
}
