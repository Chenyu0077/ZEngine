//------------------------------
// ZEngine
// 作者:
//------------------------------

using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class GamePermanceModel : BaseModel
    {
        public int Days = 13;

        public int Hours = 0;

        public string TimeLabel;

        public int TotalCount;

        public int DeadCount;

        public override void Initialize()
        {
        }

        public override void OnRelease()
        {
            
        }
    }
}
