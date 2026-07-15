//------------------------------
// ZEngine
// 作者:
//------------------------------

using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class ShortTipModel : BaseModel
    {
        public string Content;

        public override void Initialize() { }

        public override void OnRelease()
        {
            Content = null;
        }
    }
}
