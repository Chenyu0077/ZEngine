using System.Collections.Generic;
using Hotfix.FuncModule;
using Hotfix.Main.Leiya;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class VillageLogModel : BaseModel
    {
        /// <summary>指定要查看的 run_id，null 表示最新一次。</summary>
        public string RunId;

        /// <summary>已加载的村志数据，加载完成后由 Controller 填充。</summary>
        public ChronicleResponse Chronicle = null;

        public override void Initialize() { }
        public override void OnRelease()
        {
            Chronicle = null;
        }
    }
}
