using ZEngine.Reference;

namespace ZEngine.Manager.UI.UGUI
{
    public abstract class UBaseController : IReference
    {
        protected UBaseModel _model;
        protected UBaseView _view;

        public void SetView(UBaseView view)
        {
            _view = view;
            _model = _view.Data;
        }

        public virtual void Initialize()
        {
            //这里进行组件的绑定，以及事件的注册等操作
        }

        public virtual void OnUpdate()
        {
            //这里进行每帧的更新操作
        }

        public virtual void OnRelease()
        {
            //这里进行事件的注销等操作
            _view = null;
            _model = null;
        }
    }
}
