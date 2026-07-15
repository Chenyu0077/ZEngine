//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using cfg;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using ZEngine.Config;
using ZEngine.Manager.Resource;
namespace Hotfix.Main.Utils
{
    /// <summary>
    /// 导表信息读取
    /// </summary>
    public class TableMgr
    {
        private static TableMgr _I;
        public static TableMgr I
        {
            get
            {
                if (_I == null)
                {
                    _I = new TableMgr();
                }
                return _I;
            }
        }

        private Tables _tables;
        public Tables Tables
        {
            get
            {
                if (_tables == null)
                    Init();
                return _tables;
            }
        }

        public void Init()
        {
            _tables = new cfg.Tables(LoadJson);
        }

        private JArray LoadJson(string file)
        {
            TextAsset asset = (TextAsset)ResourceManager.Instance.LoadAssetSync<TextAsset>(HotfixAssetPaths.Config_Json + file + ".json").AssetObject;
            if(asset != null)
            {
                Debug.Log($"LoadJson: {file} success");
                return JsonConvert.DeserializeObject(asset.text) as JArray;
            }
            else
            {
                Debug.LogError($"LoadJson: {file} failed, asset is null");
                return null;
            }
        }




        
    }
}
