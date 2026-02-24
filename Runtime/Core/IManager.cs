//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

namespace ZEngine.Core
{
    public interface IManager
    {
        /// <summary>
        /// 初始化模块
        /// </summary>
        void OnInit(object param);

        /// <summary>
        /// 轮询模块
        /// </summary>
        void OnUpdate();

        /// <summary>
        /// 销毁模块
        /// </summary>
        void OnDestroy();

        /// <summary>
        /// GUI绘制
        /// </summary>
        void OnGUI();
    }
}
