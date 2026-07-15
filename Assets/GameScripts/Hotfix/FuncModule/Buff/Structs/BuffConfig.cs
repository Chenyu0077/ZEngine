//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Hotfix.FuncModule
{
    /// <summary>
    /// 存储所有的BuffData（ScriptableObject）
    /// </summary>
    [CreateAssetMenu(menuName = "CreateCustomSO/BuffConfig")]
    public class BuffConfig : ScriptableObject
    {
        public List<BuffData> buffDatas = new List<BuffData>();


        [Button("收集")]
        public void CollectAllBuffDatas()
        {
#if UNITY_EDITOR
            buffDatas.Clear();
            string[] guids = AssetDatabase.FindAssets("t:BuffData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                BuffData data = AssetDatabase.LoadAssetAtPath<BuffData>(path);
                if (data != null && !buffDatas.Contains(data))
                {
                    buffDatas.Add(data);
                }
            }

            buffDatas.Sort((a, b) => 
            {
                if (a.buff_id > b.buff_id)
                    return 1;
                else if (a.buff_id == b.buff_id)
                    return 0;
                else
                    return -1;
            });
#endif
        }
    }
}
