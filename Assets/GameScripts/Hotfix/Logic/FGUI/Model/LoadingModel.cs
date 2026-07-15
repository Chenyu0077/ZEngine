//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class LoadingModel : BaseModel
    {
        public bool CanLoaded;   // 能否加载完成

        public Action OnLoadingCompleted;   // 进度加载完成回调

        public override void Initialize()
        {
            CanLoaded = false;
        }

        public override void OnRelease()
        {
            
        }
    }
}
