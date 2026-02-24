//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using ZEngine.Core;

namespace ZEngine.AI.FSM
{
    /// <summary>
	/// 复合节点：包含子 FSM
	/// </summary>
	public class FsmCompositeNode : IFsmNode
    {
        public string Name { get; private set; }

        //初始启动节点名称
        public string InitName;
        public FiniteStateMachine SubFsm { get; private set; }

        public FsmCompositeNode(string name)
        {
            Name = name;
            SubFsm = new FiniteStateMachine();
        }

        public virtual void OnEnter()
        {
            ZEngineLog.Log($"Enter Composite Node: {Name}");
            SubFsm?.RunFirst();
        }

        public virtual void OnUpdate()
        {
            SubFsm?.Update();
        }

        public virtual void OnFixedUpdate()
        {
            SubFsm?.FixedUpdate();
        }

        public virtual void OnExit()
        {
            ZEngineLog.Log($"Exit Composite Node: {Name}");
        }

        public virtual void OnHandleMessage(object msg)
        {
            SubFsm?.HandleMessage(msg);
        }
    }
}
