//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System.Collections.Generic;
using System;
using ZEngine.Config;
using System.IO;
using ZEngine.Core;
using LitJson;
using ZEngine.Manager.Resource;
using UnityEngine;

namespace ZEngine.Manager.UI
{
    public class PackageUtils
    {
        //配置文件路径
        private static string _path = GameAssetPaths.Config_UI;

        /// <summary>
        /// 获取依赖包
        /// </summary>
        /// <param name="pkgName"></param>
        public static List<string> GetDependencies(string pkgName)
        {
            string json = "";
            try
            {
                var handle = ResourceManager.Instance.LoadAssetSync<TextAsset>(_path);
                json = (handle.AssetObject as TextAsset).text;
            }
            catch (Exception ex)
            {
                ZEngineLog.Error("读取发生错误：" + ex.Message);
            }

            Dictionary<string, List<string>> _dependencies = JsonMapper.ToObject<Dictionary<string, List<string>>>(json);
            if (_dependencies.ContainsKey(pkgName))
                return _dependencies[pkgName];

            return null;
        }
    }
}
