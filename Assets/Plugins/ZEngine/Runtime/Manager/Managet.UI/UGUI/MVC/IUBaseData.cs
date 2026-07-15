using System;
using ZEngine.Reference;

namespace ZEngine.Manager.UI.UGUI
{
    public interface IUBaseData : IReference
    {
        public UBaseModel Data { get; set; }

        public Action<UBaseModel> OnChanged { get; set; }
    }
}
