//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using ZEngine.Reference;

namespace ZEngine.Manager.UI
{
    public interface IBaseData : IReference
    {
        public BaseModel Data { get; set; }

        public Action OnChanged { get; set; }
    }
}
