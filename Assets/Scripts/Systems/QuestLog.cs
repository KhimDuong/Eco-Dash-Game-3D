using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Lifecycle of one tracked quest.</summary>
public enum QuestState { Hidden = 0, Active = 1, Completed = 2 }

/// <summary>
/// The general multi-quest store (M9, K5) — generalizes M8's single-quest
/// <see cref="QuestProgress"/> to many quests, each with a state and an optional
/// progress counter, plus a set of crafting-unlock <em>flags</em> granted on quest
/// completion. A static, JSON-persisted store (via <see cref="SaveSystem"/>) like
/// <see cref="Inventory"/>; quest givers (K9) drive it and the quest-log UI / crafting
/// listen to <see cref="OnChanged"/>. The M8 antidote quest stays in
/// <see cref="QuestProgress"/>; this only bridges its completion to the large-heal
/// recipe flag (see <see cref="SyncLegacy"/>).
/// </summary>
public static class QuestLog
{
    // Crafting-unlock flags (recipes check these; see QuestCatalog grants + Crafting).
    public const string FlagRecipeAdvanced = "recipe_advanced";   // Ông Bear: shield + seed bomb
    public const string FlagRecipePortal   = "recipe_portal";     // Cô Lan: portal shard
    public const string FlagRecipeLargeHeal = "recipe_largeheal"; // Ông Sáu (post-antidote): large heal

    const string SaveKey = "eco_questlog";

    /// <summary>Raised on any quest/flag change (log UI + crafting listen).</summary>
    public static event Action OnChanged;

    [Serializable] class Entry { public string id; public int state; public int progress; }
    [Serializable] class SaveData { public List<Entry> quests = new List<Entry>(); public List<string> flags = new List<string>(); }

    static SaveData data;

    // --- Queries ------------------------------------------------------------

    public static QuestState GetState(string id)
    {
        EnsureLoaded();
        var e = Find(id);
        return e == null ? QuestState.Hidden : (QuestState)e.state;
    }

    public static int GetProgress(string id)
    {
        EnsureLoaded();
        var e = Find(id);
        return e == null ? 0 : e.progress;
    }

    public static bool IsActive(string id) => GetState(id) == QuestState.Active;
    public static bool IsCompleted(string id) => GetState(id) == QuestState.Completed;

    public static bool HasFlag(string flag)
    {
        EnsureLoaded();
        if (data.flags.Contains(flag)) return true;
        // Legacy bridge: the large-heal recipe is learned from Ông Sáu the moment Tí is
        // saved (M8). QuestLog caches state, so resolve this lazily on check too — not
        // just at load — or it would stay locked for the rest of the session.
        if (flag == FlagRecipeLargeHeal && QuestProgress.Stage == QuestStage.TiSaved)
        {
            if (AddFlag(flag)) Save();
            return true;
        }
        return false;
    }

    /// <summary>Ids of quests the player has seen (Active or Completed), for the log UI.</summary>
    public static IEnumerable<string> KnownQuestIds()
    {
        EnsureLoaded();
        foreach (var e in data.quests) yield return e.id;
    }

    // --- Mutations ----------------------------------------------------------

    /// <summary>Offer a quest (Hidden → Active). No-op if already started/done.</summary>
    public static void Offer(string id)
    {
        EnsureLoaded();
        var e = GetOrCreate(id);
        if (e.state != (int)QuestState.Hidden) return;
        e.state = (int)QuestState.Active;

        // Back-fill counter quests from progress the player already made before
        // accepting, so e.g. lore notes found before meeting Cô Lan still count —
        // otherwise the quest could become uncompletable.
        var def = QuestCatalog.Get(id);
        if (def != null && def.backfillStageNotes > 0)
        {
            e.progress = Codex.LoreNotesInStage(def.backfillStageNotes);
            TryAutoComplete(e);
        }
        Changed();
    }

    public static void SetProgress(string id, int value)
    {
        EnsureLoaded();
        var e = GetOrCreate(id);
        e.progress = Mathf.Max(0, value);
        TryAutoComplete(e);
        Changed();
    }

    public static void AddProgress(string id, int amount = 1)
    {
        EnsureLoaded();
        var e = GetOrCreate(id);
        if (e.state != (int)QuestState.Active) return;
        e.progress += amount;
        TryAutoComplete(e);
        Changed();
    }

    /// <summary>Mark a quest complete and grant its catalog unlock flag.</summary>
    public static void Complete(string id)
    {
        EnsureLoaded();
        var e = GetOrCreate(id);
        if (e.state == (int)QuestState.Completed) return;
        e.state = (int)QuestState.Completed;
        var def = QuestCatalog.Get(id);
        if (def != null && !string.IsNullOrEmpty(def.grantFlag)) AddFlag(def.grantFlag);
        Changed();
    }

    public static void SetFlag(string flag)
    {
        EnsureLoaded();
        if (AddFlag(flag)) Changed();
    }

    /// <summary>Wipe all quest progress (New Game — via <see cref="SaveSystem.ResetNewGame"/>).</summary>
    public static void ResetAll()
    {
        data = new SaveData();
        Save();
        OnChanged?.Invoke();
    }

    // --- Internals ----------------------------------------------------------

    static void TryAutoComplete(Entry e)
    {
        if (e.state != (int)QuestState.Active) return;
        var def = QuestCatalog.Get(e.id);
        if (def != null && def.target > 0 && e.progress >= def.target) Complete(e.id);
    }

    static bool AddFlag(string flag)
    {
        if (string.IsNullOrEmpty(flag) || data.flags.Contains(flag)) return false;
        data.flags.Add(flag);
        return true;
    }

    static Entry Find(string id) => data.quests.Find(q => q.id == id);

    static Entry GetOrCreate(string id)
    {
        var e = Find(id);
        if (e == null) { e = new Entry { id = id, state = 0, progress = 0 }; data.quests.Add(e); }
        return e;
    }

    static void Changed() { Save(); OnChanged?.Invoke(); }

    static void EnsureLoaded()
    {
        if (data != null) return;
        data = SaveSystem.LoadJson<SaveData>(SaveKey);
        SyncLegacy();
    }

    // Bridge M8: once Tí is saved, the large-heal recipe is learned from Ông Sáu.
    static void SyncLegacy()
    {
        if (QuestProgress.Stage == QuestStage.TiSaved && AddFlag(FlagRecipeLargeHeal))
            Save();
    }

    static void Save() => SaveSystem.SaveJson(SaveKey, data);
}
