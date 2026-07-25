using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sổ Tay Greenie (codex/journal) + the per-stage cleaning meter (M9, K6). A static,
/// JSON-persisted store (via <see cref="SaveSystem"/>) holding three things:
/// <list type="bullet">
/// <item><b>Bestiary</b> — enemy ids recorded on first kill.</item>
/// <item><b>Lore notes</b> — note ids found across the stages.</item>
/// <item><b>Cleanliness</b> — a 0–100% "Độ Sạch" per stage, raised by Anh's ground
/// cleanse and rewarded at 50% / 100%.</item>
/// </list>
/// <para><b>Cross-team contract (m9-tasks §0):</b> Anh's <c>GroundCleanser</c> calls
/// <see cref="AddCleanliness"/>; Anh's milestone VFX (A3) subscribe to
/// <see cref="OnCleanlinessChanged"/>. Khiêm's reward hook (here) grants the actual
/// 50% / 100% rewards once each.</para>
/// </summary>
public static class Codex
{
    public const int StageValley = 1;   // Stage 1 — Barren Farm / valley
    public const int StageFactory = 2;  // Stage 2 — Factory maze

    const string SaveKey = "eco_codex";

    /// <summary>Raised when the bestiary or lore-note set changes (codex UI listens).</summary>
    public static event Action OnChanged;

    /// <summary>(stageId, newPercent 0–100). Anh's milestone VFX + the tracker listen.</summary>
    public static event Action<int, float> OnCleanlinessChanged;

    [Serializable] class StageClean { public int stageId; public float pct; public bool reward50; public bool reward100; }
    [Serializable]
    class SaveData
    {
        public List<string> bestiary = new List<string>();
        public List<string> loreNotes = new List<string>();
        public List<StageClean> stages = new List<StageClean>();
    }

    static SaveData data;

    // --- Bestiary -----------------------------------------------------------

    /// <summary>Record an enemy kill; returns true if it's a new bestiary entry.</summary>
    public static bool RecordKill(string enemyId)
    {
        if (string.IsNullOrEmpty(enemyId)) return false;
        EnsureLoaded();
        if (data.bestiary.Contains(enemyId)) return false;
        data.bestiary.Add(enemyId);
        Save();
        OnChanged?.Invoke();
        return true;
    }

    public static bool HasSeen(string enemyId) { EnsureLoaded(); return data.bestiary.Contains(enemyId); }
    public static int BestiaryCount { get { EnsureLoaded(); return data.bestiary.Count; } }

    // --- Lore notes ---------------------------------------------------------

    /// <summary>Record a found lore note; returns true if it's newly found.</summary>
    public static bool FindLoreNote(string noteId)
    {
        if (string.IsNullOrEmpty(noteId)) return false;
        EnsureLoaded();
        if (data.loreNotes.Contains(noteId)) return false;
        data.loreNotes.Add(noteId);
        Save();
        OnChanged?.Invoke();
        return true;
    }

    public static bool HasLoreNote(string noteId) { EnsureLoaded(); return data.loreNotes.Contains(noteId); }
    public static int LoreNoteCount { get { EnsureLoaded(); return data.loreNotes.Count; } }
    public static IEnumerable<string> FoundLoreNotes() { EnsureLoaded(); return data.loreNotes; }

    /// <summary>How many found lore notes belong to the given stage (for quest back-fill).</summary>
    public static int LoreNotesInStage(int stageId)
    {
        EnsureLoaded();
        int n = 0;
        foreach (var id in data.loreNotes)
        {
            var note = LoreNoteCatalog.Get(id);
            if (note != null && note.stageId == stageId) n++;
        }
        return n;
    }

    // --- Cleanliness --------------------------------------------------------

    public static float GetCleanliness(int stageId)
    {
        EnsureLoaded();
        var s = data.stages.Find(x => x.stageId == stageId);
        return s == null ? 0f : s.pct;
    }

    /// <summary>
    /// Raise a stage's cleanliness by <paramref name="amount"/> percent (clamped to
    /// 100). Fires <see cref="OnCleanlinessChanged"/> and grants the 50% / 100%
    /// rewards once each. Called by Anh's <c>GroundCleanser</c> on each cleanse.
    /// </summary>
    public static void AddCleanliness(int stageId, float amount)
    {
        if (amount <= 0f) return;
        EnsureLoaded();
        var s = data.stages.Find(x => x.stageId == stageId);
        if (s == null) { s = new StageClean { stageId = stageId }; data.stages.Add(s); }

        float before = s.pct;
        s.pct = Mathf.Clamp(s.pct + amount, 0f, 100f);
        if (Mathf.Approximately(before, s.pct)) return;

        if (!s.reward50 && s.pct >= 50f) { s.reward50 = true; GrantReward(stageId, 50); }
        if (!s.reward100 && s.pct >= 100f) { s.reward100 = true; GrantReward(stageId, 100); }

        Save();
        OnCleanlinessChanged?.Invoke(stageId, s.pct);
    }

    // 50% / 100% "valley bloom" rewards. The cosmetic bloom VFX is Anh's A3 (listens
    // to OnCleanlinessChanged); this grants the material/shard payoff.
    static void GrantReward(int stageId, int threshold)
    {
        if (threshold >= 100)
        {
            Inventory.TryAdd("portal_shard", 1);
            Inventory.TryAdd("energy_shard", 2);
        }
        else // 50%
        {
            Inventory.TryAdd("bottle", 3);
            Inventory.TryAdd("scrap", 3);
        }
    }

    // --- Persistence --------------------------------------------------------

    public static void ResetAll()
    {
        data = new SaveData();
        Save();
        OnChanged?.Invoke();
        OnCleanlinessChanged?.Invoke(StageValley, 0f);
        OnCleanlinessChanged?.Invoke(StageFactory, 0f);
    }

    static void EnsureLoaded()
    {
        if (data == null) data = SaveSystem.LoadJson<SaveData>(SaveKey);
    }

    static void Save() => SaveSystem.SaveJson(SaveKey, data);
}
