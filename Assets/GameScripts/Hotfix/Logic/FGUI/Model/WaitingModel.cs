using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class WaitingModel : BaseModel
    {
        public string WaitContent;

        public override void Initialize()
        {
            WaitContent = "等待中......";
        }

        public override void OnRelease()
        {
            
        }
    }
}