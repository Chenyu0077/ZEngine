//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

namespace ZEngine.AI.FSM
{
    public interface IFsmNode
    {
        /// <summary>
        /// 节点名称
        /// </summary>
        string Name { get; }

        void OnEnter();
        void OnUpdate();
        void OnFixedUpdate();
        void OnExit();
        void OnHandleMessage(object msg);

        /// <summary>
		/// 如果该节点包含子状态机，返回子 FSM；否则返回 null
		/// </summary>
		FiniteStateMachine SubFsm { get; }
    }
}
