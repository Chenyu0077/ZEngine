//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

namespace Hotfix.Core
{
    public interface IHotUpdateModule
    {
        int Priority => 0;
        void Initialize();
        void Update();
        void LateUpdate();
        void FixedUpdate();
        void Destroy();
    }
}
