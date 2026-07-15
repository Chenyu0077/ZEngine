using Hotfix.Main.Leiya;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class TaxModel : BaseModel
    {
        public TaxStateResponse    StateData    { get; set; }   // 当前期赋税面板
        public TaxFamiliesResponse FamiliesData { get; set; }
        public TaxHistoryResponse  HistoryData  { get; set; }
        public TaxConfigResponse   ConfigData   { get; set; }   // 玩法常量（税败文案、阈值等）
        public bool                IsSettling   { get; set; }

        public override void Initialize() { }

        public override void OnRelease()
        {
            StateData    = null;
            FamiliesData = null;
            HistoryData  = null;
            ConfigData   = null;
            IsSettling   = false;
        }

        public static string FormatOutcome(string outcome) => outcome switch
        {
            "full"         => "[color=#44ff88]达标[/color]",
            "minor_short"  => "[color=#ffdd00]小欠[/color]",
            "major_short"  => "[color=#ff8800]大欠·记过[/color]",
            "severe_short" => "[color=#ff4444]巨欠·结局[/color]",
            "debt_crush"   => "[color=#cc0000]债压垮·结局[/color]",
            _              => outcome ?? "",
        };

        public static string FormatTier(string tier) => tier switch
        {
            "landless"       => "无地户",
            "yeoman"         => "自耕农",
            "landlord_minor" => "小地主",
            "landlord_major" => "大地主",
            _                => tier ?? "",
        };
    }
}
