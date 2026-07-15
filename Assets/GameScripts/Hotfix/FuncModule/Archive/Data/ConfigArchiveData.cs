using MessagePack;

namespace Hotfix.FuncModule
{
    [MessagePackObject]
    public class ConfigArchiveData
    {
        [Key(0)]  public int    CommonModelIndex   { get; set; }
        [Key(1)]  public int    ReflectModelIndex  { get; set; }
        [Key(2)]  public int    RegisterModelIndex { get; set; }
        [Key(3)]  public int    PresetModeIndex    { get; set; }
        [Key(4)]  public bool   UseLlm             { get; set; }
        [Key(5)]  public string CustomBaseUrl      { get; set; } = string.Empty;
        [Key(6)]  public string CustomApiKey       { get; set; } = string.Empty;
    }
}