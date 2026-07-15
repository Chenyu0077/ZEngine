using System.Collections.Generic;
using MessagePack;

namespace Hotfix.FuncModule
{
    /// <summary>
    /// 地图运行时存档数据。
    /// 只存储与 Map Editor JSON 初始值不同的格子属性（增量 delta），
    /// 加载时先读静态 JSON，再叠加 delta，保持存档体积最小。
    /// </summary>
    [MessagePackObject]
    public class MapArchiveData
    {
        /// <summary>绑定的地图 ID，对应 Resources/Configs/Maps/{MapId}.json。</summary>
        [Key(0)] public string MapId { get; set; }

        /// <summary>
        /// 与 JSON 初始值不同的格子列表（稀疏，只存变化项）。
        /// 加载时由各系统按需调用 MapLoader.SetCellProp() 应用。
        /// </summary>
        [Key(1)] public List<CellDelta> CellDeltas { get; set; } = new List<CellDelta>();
    }

    /// <summary>
    /// 单格属性增量。只记录实际发生过变化的属性，
    /// 未出现的属性保持 JSON 默认值。
    /// </summary>
    [MessagePackObject]
    public class CellDelta
    {
        [Key(0)] public int    X          { get; set; }
        [Key(1)] public int    Y          { get; set; }
        /// <summary>格子是否可行走（null = 未变化，保持 JSON 默认）。</summary>
        [Key(2)] public bool?  Walkable   { get; set; }
        /// <summary>格子是否可建造。</summary>
        [Key(3)] public bool?  Buildable  { get; set; }
        /// <summary>格子是否可耕种。</summary>
        [Key(4)] public bool?  Farmable   { get; set; }
    }
}
