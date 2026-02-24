//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using MessagePack;

namespace ZEngine.Module.Archive
{
    /// <summary>
    /// 存档槽位基类
    /// </summary>
    public class ArchiveSlotBase
    {
        [IgnoreMember] public virtual string ID { get; set; }         //存档槽位唯一ID
        [IgnoreMember] public virtual string TimeStamp { get; set; }  //时间戳
        [IgnoreMember] public virtual string SaveTime { get; set; }   //保存时间
        [IgnoreMember] public virtual string SlotName { get; set; }   //存档名字

        public virtual void Init(string gameVersion, int schemaVersion) { }

        public virtual void BeforeSerialize() { }

        public virtual void AfterDeserializae(string gameVersion, int schemaVersion) { }

        public string GetFileName()
        {
            return $"{SlotName}_{ID}";
        }
    }
}
