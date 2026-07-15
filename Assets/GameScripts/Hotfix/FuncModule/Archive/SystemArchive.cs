//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using MessagePack;

namespace Hotfix.FuncModule
{
    /// <summary>
    /// 系统存档（单机游戏的系统设置存档）
    /// </summary>
    [MessagePackObject]
    public partial class SystemArchive : ArchiveBase
    {
        [Key(3)] public ConfigArchiveData ConfigData = new ConfigArchiveData();

        public override void InitOnce() { }

        protected override void OnApplicationVersionChange(string oldVersion, string newVersion) { }
    }
}
