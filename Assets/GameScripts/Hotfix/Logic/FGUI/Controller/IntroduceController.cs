//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using FairyGUI;
using Hotfix.UI.Generate.Main;
using UnityEngine;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class IntroduceController : BaseController
    {
        private UIIntroduceView compt;

        public override void Initialize()
        {
            base.Initialize();
            compt = _view.GetView() as UIIntroduceView;

            if (compt != null)
            {
                compt.m_CloseBtn.onClick.Add(OnCloseBtnEvent);
            }

            _view.OnChanged = (data) =>
            {

            };
        }

        private void OnCloseBtnEvent(EventContext context)
        {
            Debug.Log("关闭介绍界面");
            UIManager.Instance.CloseView<IntroduceView>();
        }

        public override void OnRelease()
        {
            if (compt != null)
            {
                compt.m_CloseBtn.onClick.Remove(OnCloseBtnEvent);
                compt = null;
            }
            base.OnRelease();
        }
    }
}
