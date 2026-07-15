//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using UnityEngine;
using ZEngine.Core;
using ZEngine.Manager.Event;
using ZEngine.Manager.Log;
using ZEngine.Manager.Resource;

namespace ZEngine.Manager.Localization
{
    /// <summary>
    /// 本地化管理器
    /// UGUI:
    /// 1.文本通过表格或其他文件注册配置，文本需要绑定 LocalizationTextBind 组件
    /// 2.图片等资源是通过地址注册实现，切换语言，获取对应图片的地址，再根据地址加载图片
    /// FGUI:
    /// 1.文本通过表格或其他文件注册配置，直接获取对应文本，通过代码给UI组件赋值
    /// 2.图片资源是通过FGUI编辑器打的图集，注册时通过FGUI分配的id或名字注册，获取也一样，其他资源获取于UGUI一样
    /// </summary>
    public class LocalizationManager : ManagerSingleton<LocalizationManager>, IManager
    {
        private Language _currentLanguage;    // 当前语言
        private string[] _languages;          // 所有语言种类

        /// <summary>
        /// 获取本地化文本的委托（参数：fieldName, language，返回：文本）
        /// </summary>
        private Func<string, string, string> _getTextFunc;
        /// <summary>
        /// 获取本地化字体的委托（参数：language，返回：字体资源路径）
        /// </summary>
        private Func<string, string> _getFontFunc;
        /// <summary>
        /// 获取本地化资源路径的委托（参数：assetName, language，返回：资源路径）
        /// </summary>
        private Func<string, string, string> _getAssetFunc;


        #region 生命周期
        public void OnInit(object param)
        {
            //检测依赖模块
            if (ZEngineMain.Contains(typeof(LogManager)) == false)
                throw new Exception($"{nameof(LocalizationManager)}依赖于{nameof(LogManager)}");
            if (ZEngineMain.Contains(typeof(ResourceManager)) == false)
                throw new Exception($"{nameof(LocalizationManager)}依赖于{nameof(ResourceManager)}");
            if (ZEngineMain.Contains(typeof(EventManager)) == false)
                throw new Exception($"{nameof(LocalizationManager)}依赖于{nameof(EventManager)}");

            _root = new GameObject("[Z][LocalizationManager]");
            GameObject.DontDestroyOnLoad(_root);

            // 设置初始化语言
            _currentLanguage = Language.zh; // 默认中文
            _languages = Enum.GetNames(typeof(Language));

        }

        public void OnUpdate()
        {

        }

        public void OnGUI()
        {

        }

        public void OnDestroy()
        {
            _getTextFunc = null;
            _getFontFunc = null;

            DestroySingleton();
        }
        #endregion


        public void SetLocalizationData(Func<string, string, string> textFunc, Func<string, string> fontFunc)
        {
            _getTextFunc = textFunc;
            _getFontFunc = fontFunc;
        }

        /// <summary>
        /// 切换语言
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        public bool ChangeLanguage(Language language)
        {
            if (_currentLanguage != language)
                _currentLanguage = language;

            // 更新文本显示
            EventManager.Instance.SendMessage(new LocalizationMsg(language));

            LogManager.Instance.Info($"切换语言: {language}");
            return true;
        }

        /// <summary>
        /// 获取当前语言
        /// </summary>
        /// <returns></returns>
        public string GetCurrentLanguage()
        {
            return _currentLanguage.ToString();
        }

        /// <summary>
        /// 获取全部语言类型
        /// </summary>
        /// <returns></returns>
        public string[] GetAllLanguages()
        {
            return _languages;
        }

        /// <summary>
        /// 获取本地化文本
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public string GetText(string fieldName)
        {
            if(_getTextFunc == null)
            {
                LogManager.Instance.Warning($"LocalizationManager: 获取文本失败，未设置获取文本的委托");
                return $"[Missing Text: {fieldName}]";
            }

            string text = _getTextFunc(fieldName, _currentLanguage.ToString());
            if(string.IsNullOrEmpty(text))
            {
                LogManager.Instance.Warning($"LocalizationManager: 获取文本失败，文本为空，字段名: {fieldName}");
                return $"[Missing Text: {fieldName}]";
            }
            return text;
        }

        /// <summary>
        /// 获取字体资源路径
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        public string GetFontPath()
        {
            if(_getFontFunc == null)
            {
                LogManager.Instance.Warning($"LocalizationManager: 获取字体失败，未设置获取字体的委托");
                return null;
            }

            string font = _getFontFunc(_currentLanguage.ToString());
            if(string.IsNullOrEmpty(font))
            {
                LogManager.Instance.Warning($"LocalizationManager: 获取字体失败，字体路径为空，语言: {_currentLanguage.ToString()}");
                return null;
            }

            return font;
        }
        
        /// <summary>
        /// 获取本地化资源
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public string GetAssetPath(string assetName)
        {
            if(_getAssetFunc == null)
            {
                LogManager.Instance.Warning($"LocalizationManager: 获取资源路径失败，未设置获取资源路径的委托");
                return null;
            }

            string path = _getAssetFunc(assetName, _currentLanguage.ToString());
            if(string.IsNullOrEmpty(path))
            {
                LogManager.Instance.Warning($"LocalizationManager: 获取资源路径失败，文本为空，资源名: {assetName}");
                return null;
            }
            return path;
        }
    }
}
