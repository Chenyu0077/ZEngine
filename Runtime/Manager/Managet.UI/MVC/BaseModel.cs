//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using ZEngine.Manager.Event;
using ZEngine.Reference;

namespace ZEngine.Manager.UI
{
    public abstract class BaseModel : IReference
    {
        public abstract void Initialize();

        public abstract void OnRelease();
    }
}
