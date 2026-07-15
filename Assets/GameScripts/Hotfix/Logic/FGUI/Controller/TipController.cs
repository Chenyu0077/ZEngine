//------------------------------
// ZEngine
// 作者:
//------------------------------

using System;
using FairyGUI;
using Hotfix.UI.Generate.Main;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class TipController : BaseController
    {
        private UITipView _compt;
        private TipModel _model;

        public override void Initialize()
        {
            base.Initialize();
            _compt = _view.GetView() as UITipView;
            _model = _view.Data as TipModel;
            if (_model == null || string.IsNullOrEmpty(_model.TipContent))
                _compt.m_content.text = "提示内容为空";
            else
                _compt.m_content.text = _model.TipContent;

            _compt.m_closeBtn.onClick.Add(OnBtnCloseEvent);
        }

        private void OnBtnCloseEvent(EventContext context)
        {
            UIManager.Instance.CloseView<TipView>();
        }


        public override void OnRelease()
        {
            if (_compt != null)
            {
                _compt.m_closeBtn.onClick.Remove(OnBtnCloseEvent);
                _compt = null;
            }

            base.OnRelease();
        }
    }
}
