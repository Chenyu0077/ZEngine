//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using MessagePack;

namespace ZEngine.Module.Archive
{
    [MessagePackObject]
    public class SlotInfo
    {
        [Key(0)] public string ID { get; set; }          // 存档槽位唯一ID
        [Key(1)] public string SlotName { get; set; }    // 存档名称
        [Key(2)] public string SaveTime { get; set; }    // 存档时间

        public string GetFileName()
        {
            return $"{SlotName}_{ID}";
        }
    }
}
