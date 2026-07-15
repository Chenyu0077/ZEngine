//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using Hotfix.Main.Logic.Nodes;
using ZEngine.AI.FSM;

namespace Hotfix.Main.Logic
{
    public class ModuleMgr
    {
        private static ModuleMgr _I;
        public static ModuleMgr I
        {
            get
            {
                if (_I == null)
                {
                    _I = new ModuleMgr();
                }
                return _I;
            }
        }

        public void Initialize()
        {
            Fsm = new FiniteStateMachine();
            Fsm.AddNode(new InitNode());
            Fsm.AddNode(new WorldSpawnNode());
            Fsm.AddNode(new PrepareNode());
            Fsm.AddNode(new BattleNode());
            Fsm.AddNode(new SettlementNode());
        }

        public FiniteStateMachine Fsm { get; set; }
    }
}
