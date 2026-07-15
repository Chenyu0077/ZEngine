//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZEngine.Manager.Event;
using ZEngine.Manager.Resource;

namespace ZEngine.Manager.Localization
{
    /// <summary>
    /// 该绑定组件适用于UGUI
    /// </summary>
    public class LocalizationTextBind : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _tmpComponent;
        [SerializeField]
        private Text _textComponent;
        [SerializeField]
        private Language _currentLanguage;  // 当前语言
        [SerializeField]
        private string _fieldName;  // 字段名



        private void Awake()
        {
            _tmpComponent = GetComponent<TextMeshProUGUI>();
            _textComponent = GetComponent<Text>();
        }


        private void OnEnable()
        {
            EventManager.Instance.AddListener<LocalizationMsg>(OnLocalizationEvent);
        }

        private void OnDisable()
        {
            EventManager.Instance.RemoveListener<LocalizationMsg>(OnLocalizationEvent);
        }

        /// <summary>
        /// 重新设置文本内容和字体
        /// </summary>
        /// <param name="message"></param>
        private void OnLocalizationEvent(IEventMessage message)
        {
            var msg = message as LocalizationMsg;
            if(msg != null)
            {
                if (msg._language != _currentLanguage)
                {
                    _currentLanguage = msg._language;
                    string text = LocalizationManager.Instance.GetText(_fieldName);
                    string fontPath = LocalizationManager.Instance.GetFontPath();
                    if (_tmpComponent != null)
                    {
                        _tmpComponent.text = text;
                        _tmpComponent.font = ResourceManager.Instance.LoadAssetSync<TMP_Asset>(fontPath).AssetObject as TMP_FontAsset;
                    }
                    if (_textComponent != null)
                    {
                        _textComponent.text = text;
                        _textComponent.font = ResourceManager.Instance.LoadAssetSync<Font>(fontPath).AssetObject as Font;
                    }
                }
            }
        }


        /// <summary>
        /// 获取当前使用的文本内容
        /// </summary>
        /// <returns></returns>
        public string GetCurrentText()
        {
            if (_tmpComponent != null)
                return _tmpComponent.text;
            if (_textComponent != null)
                return _textComponent.text;
            return string.Empty;
        }

    }
}
