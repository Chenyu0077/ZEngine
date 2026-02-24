//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using Cysharp.Threading.Tasks;
using FairyGUI;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ZEngine.Core;
namespace ZEngine.Module.Archive

{
    /// <summary>
    /// 存档管理器（单机游戏的存档管理）
    /// </summary>
    public class ArchiveManager : ManagerSingleton<ArchiveManager>, IManager
    {
        private string SavePath = Application.persistentDataPath + "/Saves/";        //存档目录
        private string SlotFilePath = "slotinfos";      //存档文件前缀
        private const int LastestSchemaVersion = 0;     //当前Schema版本
        private const string CurrentGameVersion = "1.0"; //当前游戏版本
        private Dictionary<string, ArchiveSlotBase> slotDic;
        private List<SlotInfo> slotInfos;
        private int currentSlotIndex = 0;   //当前存档槽位索引


        public void OnInit(object param)
        {
            _root = new GameObject("[Z][ArchiveManager]");
            GameObject.DontDestroyOnLoad(_root);

            Debug.Log(SavePath);
            slotDic = new Dictionary<string, ArchiveSlotBase>();

            if (!Directory.Exists(SavePath))
                Directory.CreateDirectory(SavePath);

            Debug.Log($"本地存档地址：{GetSlotInfosPath()}");
            if (!File.Exists(GetSlotInfosPath()))
            {
                slotInfos = new List<SlotInfo>();
                File.WriteAllBytes(GetSlotInfosPath(), MessagePackSerializer.Serialize<List<SlotInfo>>(slotInfos));
            }
            else
            {
                try
                {
                    var bytes = File.ReadAllBytes(GetSlotInfosPath());
                    slotInfos = MessagePackSerializer.Deserialize<List<SlotInfo>>(bytes);
                    Debug.Log($"存档初始化成功");
                }
                catch (Exception e)
                {
                    Debug.LogError($"加载存档信息列表失败: {e}");
                    slotInfos = new List<SlotInfo>();
                }
            }
        }


        public void OnUpdate()
        {

        }

        public void OnGUI()
        {

        }

        public void OnDestroy()
        {
            DestroySingleton();
        }


        /// <summary>
        /// 创建并保存新的存档槽位
        /// </summary>
        /// <param name="index"></param>
        public SlotInfo CreateNewSlot<T>() where T : ArchiveSlotBase, new()
        {
            string id = Guid.NewGuid().ToString("N");
            T slot = new T();
            slot.ID = id;
            slot.Init(CurrentGameVersion, LastestSchemaVersion);
            slotDic.Add(id, slot);
            var slotInfo = new SlotInfo()
            {
                ID = id,
                SlotName = slot.SlotName,
                SaveTime = slot.SaveTime,
            };
            slotInfos.Add(slotInfo);
            SaveSync<T>(slot);

            //再次保存总文档目录信息
            File.WriteAllBytes(GetSlotInfosPath(), MessagePackSerializer.Serialize<List<SlotInfo>>(slotInfos));
            return slotInfo;
        }

        /// <summary>
        /// 同步保存存档
        /// </summary>
        /// <param name="index"></param>
        public void SaveSync<T>(T slot) where T : ArchiveSlotBase, new()
        {
            if (slot == null)
            {
                Debug.LogError($"slot不存在!");
                return;
            }

            // 自动执行子存档保存前的逻辑
            slot.BeforeSerialize();
            var bytes = MessagePackSerializer.Serialize<T>(slot);
            var path = GetSlotPath(slot.GetFileName());
            Directory.CreateDirectory(SavePath);
            File.WriteAllBytes(path, bytes);
        }

        /// <summary>
        /// 同步加载存档
        /// </summary>
        /// <param name="index"></param>
        public void LoadSync<T>(string fileName) where T : ArchiveSlotBase, new()
        {
            if (string.IsNullOrEmpty(fileName))
                return;

            var path = GetSlotPath(fileName);
            if (!File.Exists(path))
            {
                Debug.LogError($"存档文件不存在: {path}");
                return;
            }

            var bytes = File.ReadAllBytes(path);
            var slot = MessagePackSerializer.Deserialize<T>(bytes);

            // 自动执行子存档迁移，修复逻辑
            slot.AfterDeserializae(CurrentGameVersion, LastestSchemaVersion);
        }

        /// <summary>
        /// 获取存档槽位
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ArchiveSlotBase GetSlot(string id)
        {
            if(slotDic.TryGetValue(id, out var slot))
            {
                return slot;
            }
            Debug.LogError($"slot不存在: {id}");
            return null;
        }

        /// <summary>
        /// 获取全部存储槽位信息
        /// </summary>
        /// <returns></returns>
        public List<SlotInfo> GetAllSlotInfos()
        {
            return slotInfos;
        }

        private string GetSlotPath(string name)
        {
            return Path.Combine(SavePath, $"slot_{name}.sav");
        }

        private string GetSlotInfosPath()
        {
            return Path.Combine(SavePath, $"{SlotFilePath}.sav");
        }
    }
}
