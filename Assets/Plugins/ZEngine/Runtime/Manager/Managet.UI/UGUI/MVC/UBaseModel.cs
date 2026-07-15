using ZEngine.Reference;

namespace ZEngine.Manager.UI.UGUI
{
    public abstract class UBaseModel : IReference
    {
        public virtual void Initialize() { }

        public virtual void OnRelease() { }
    }
}
