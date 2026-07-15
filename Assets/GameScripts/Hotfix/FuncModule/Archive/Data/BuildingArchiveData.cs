using MessagePack;

namespace Hotfix.FuncModule
{
    [MessagePackObject]
    public class BuildingArchiveData
    {
        [Key(0)] public PlacedBuildingRecord[] Buildings { get; set; }
    }

    [MessagePackObject]
    public class PlacedBuildingRecord
    {
        [Key(0)] public string ConfigId { get; set; }
        [Key(1)] public int    GridX    { get; set; }
        [Key(2)] public int    GridY    { get; set; }
        [Key(3)] public int    SizeX    { get; set; }
        [Key(4)] public int    SizeY    { get; set; }
    }
}
