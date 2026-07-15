using Hotfix.UI.Generate.Main;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class WaitingController : BaseController
    {
        private UIWaitingView compt;
        private WaitingModel model;

        public override void Initialize()
        {
            base.Initialize();
            compt = _view.GetView() as UIWaitingView;
            model = _view.Data as WaitingModel;

            if (compt != null && model != null)
                compt.m_content.text = model.WaitContent;
        }

        public override void OnRelease()
        {
            compt = null;
            model = null;
            base.OnRelease();
        }
    }
}
