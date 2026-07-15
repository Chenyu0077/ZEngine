//------------------------------
// ZEngine
// 作者:
//------------------------------

using Hotfix.Main.Leiya;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class GameOverModel : BaseModel
    {
        public SimStatusResponse GameStatus  { get; set; }
        public bool              IsTaxFailure { get; set; } // true=税败结局，false=村庄/村长结局

        public override void Initialize()
        {
        }

        public override void OnRelease()
        {
        }
    }
}
