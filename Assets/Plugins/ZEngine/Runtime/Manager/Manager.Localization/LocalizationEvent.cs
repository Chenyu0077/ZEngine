//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using ZEngine.Manager.Event;

namespace ZEngine.Manager.Localization
{
    public class LocalizationMsg : IEventMessage
    {
        public Language _language;

        public LocalizationMsg(Language language)
        {
            this._language = language;
        }

        public void OnRelease()
        {
            _language = default;
        }
    }
}
