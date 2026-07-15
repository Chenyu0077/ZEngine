using Hotfix.FuncModule.AITown;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class NPCMenuModel : BaseModel
    {
        /// <summary>
        /// 当前右键菜单对应的NPC数据
        /// </summary>
        public AgentInfo AgentInfo { get; set; }

        public override void Initialize()
        {
            // NPCMenu 的数据初始化
        }

        public override void OnRelease()
        {
            // 释放资源
            AgentInfo = null;
        }
    }
}