using Hotfix.FuncModule.AITown;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class NPCCommandModel : BaseModel
    {
        public AITownAgent Agent     { get; set; }
        public string      AgentName { get; set; }
        public string      AgentId   { get; set; }

        public override void Initialize() { }

        public override void OnRelease()
        {
            Agent     = null;
            AgentName = null;
            AgentId   = null;
        }
    }
}
