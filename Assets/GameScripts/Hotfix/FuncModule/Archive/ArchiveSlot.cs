//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using MessagePack;
using ZEngine.Module.Archive;

namespace Hotfix.FuncModule
{
    /// <summary>
    /// 存档槽位
    /// </summary>
    [MessagePackObject]
    public class ArchiveSlot : ArchiveSlotBase
    {
        /// <summary>系统配置专用槽位的固定 ID 和文件名。</summary>
        public const string SystemConfigId       = "system_config";
        public const string SystemConfigSlotName = "system_config";

        [Key(0)] public override string ID { get; set; }
        [Key(1)] public override string TimeStamp { get; set; }
        [Key(2)] public override string SaveTime { get; set; }
        [Key(3)] public override string SlotName { get; set; }
        [Key(4)] public SystemArchive System { get; set; }

        public ArchiveSlot() { }

        public ArchiveSlot(string id)
        {
            ID = id;
        }


        public override void Init(string gameVersion, int schemaVersion)
        {
            System = new SystemArchive();
            System.Init(gameVersion, schemaVersion);


            TimeStamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            SaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SlotName = $"{DateTime.Now: MMddHHmm}";
        }

        public override void BeforeSerialize()
        {
            System?.BeforeSerialize();
        }

        public override void AfterDeserializae(string gameVersion, int schemaVersion)
        {
            System?.AfterDeserialize(gameVersion, schemaVersion);
        }
    }
}
