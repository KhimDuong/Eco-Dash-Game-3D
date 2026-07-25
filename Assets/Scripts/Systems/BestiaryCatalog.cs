using System.Collections.Generic;

/// <summary>
/// Display data for the codex bestiary (M9, K6): maps an enemy's codex id to its
/// Vietnamese name and description. Enemies call <see cref="Codex.RecordKill"/> with
/// their id on death; the codex UI looks names up here. Kept as a small static table
/// (like <see cref="ItemDatabase"/>'s fallbacks) so no asset authoring is needed.
/// </summary>
public static class BestiaryCatalog
{
    public class Entry { public string id; public string name; public string description; }

    // Enemy codex ids (hard-coded by each enemy script in its death path).
    public const string PlasticSlime = "plastic_slime";
    public const string PollutionFlyBot = "pollution_flybot";
    public const string MegaSmog = "mega_smog";
    public const string SlimeKing = "slime_king";

    static Dictionary<string, Entry> byId;

    public static Entry Get(string id)
    {
        EnsureLoaded();
        return byId.TryGetValue(id, out var e) ? e : null;
    }

    public static string Name(string id)
    {
        var e = Get(id);
        return e != null ? e.name : id;
    }

    public static IEnumerable<Entry> All { get { EnsureLoaded(); return byId.Values; } }

    static void EnsureLoaded()
    {
        if (byId != null) return;
        byId = new Dictionary<string, Entry>();
        Add(PlasticSlime, "Slime Nhựa", "Khối nhựa ô nhiễm biết nảy, lao thẳng vào Greenie.");
        Add(PollutionFlyBot, "Phi Cơ Ô Nhiễm", "Robot bay của Khói Đen, bắn cầu khói độc từ xa.");
        Add(SlimeKing, "Slime Chúa", "Slime đột biến khổng lồ canh giữ một Mảnh Cổng trong khu rừng độc.");
        Add(MegaSmog, "Trùm Khói Đen", "Quái vật khói khổng lồ — lá chắn ô nhiễm cuối cùng của tập đoàn.");
    }

    static void Add(string id, string name, string desc) =>
        byId[id] = new Entry { id = id, name = name, description = desc };
}
