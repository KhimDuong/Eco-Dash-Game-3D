using System.Collections.Generic;

/// <summary>
/// Static metadata for the M9 NPC-given side quests (game-design §4.7.4). The M8
/// antidote quest keeps its own detailed state machine in <see cref="QuestProgress"/>
/// and is shown in the log read-only; these are the four new quests
/// <see cref="QuestLog"/> tracks generically (state + counter + a crafting-unlock
/// flag granted on completion). All player-facing text is Vietnamese.
/// </summary>
public static class QuestCatalog
{
    public class QuestDef
    {
        public string id;
        public string title;
        public string description;
        public string giver;
        public string stage;
        /// <summary>Items to collect; 0 = a simple do/turn-in objective (no counter).</summary>
        public int target;
        /// <summary>Crafting-unlock flag set on completion (empty = none).</summary>
        public string grantFlag;
        /// <summary>If &gt; 0, on offer the counter is back-filled from Codex lore notes
        /// already found in this stage (so they still count). 0 = no back-fill.</summary>
        public int backfillStageNotes;
    }

    // Quest ids (referenced by NPCs in K9 and the hub spawns in K7).
    public const string MayPet     = "may_pet";
    public const string TaiPond    = "tai_pond";
    public const string LanIntel   = "lan_intel";
    public const string BearRecycle = "bear_recycle";

    static Dictionary<string, QuestDef> byId;

    public static QuestDef Get(string id)
    {
        EnsureLoaded();
        return byId.TryGetValue(id, out var d) ? d : null;
    }

    public static IEnumerable<QuestDef> All { get { EnsureLoaded(); return byId.Values; } }

    static void EnsureLoaded()
    {
        if (byId != null) return;
        byId = new Dictionary<string, QuestDef>();
        Add(new QuestDef
        {
            id = MayPet, title = "Người Bạn Nhỏ Của Bé Mây", giver = "Bé Mây", stage = "Thung Lũng",
            description = "Tìm lại robot-thú cưng bị lạc của Bé Mây ở góc thung lũng (có Slime canh giữ).",
            target = 0, grantFlag = "",
        });
        Add(new QuestDef
        {
            id = TaiPond, title = "Làm Sạch Ao Độc", giver = "Ông Tài", stage = "Thung Lũng",
            description = "Dọn sạch rác ở ao độc giúp Ông Tài câu cá trở lại. Phần thưởng: 1 Mảnh Cổng.",
            target = 0, grantFlag = "",
        });
        Add(new QuestDef
        {
            id = LanIntel, title = "Tin Tức Từ Bên Trong", giver = "Cô Lan", stage = "Nhà Máy",
            description = "Thu thập 3 mẩu nhật ký trong nhà máy cho Cô Lan để vạch trần kế hoạch của Khói Đen.",
            target = 3, grantFlag = QuestLog.FlagRecipePortal, backfillStageNotes = Codex.StageFactory,
        });
        Add(new QuestDef
        {
            id = BearRecycle, title = "Tái Chế Nâng Cao", giver = "Ông Bear", stage = "Trạm Trung Tâm",
            description = "Mang 10 Mảnh Kim Loại và 10 Chai Nhựa cho Ông Bear để mở khóa công thức chế tạo nâng cao.",
            target = 0, grantFlag = QuestLog.FlagRecipeAdvanced,
        });
    }

    static void Add(QuestDef d) => byId[d.id] = d;
}
