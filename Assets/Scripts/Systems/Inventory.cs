using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One stack of items in the bag: an item <see cref="id"/> and a count.</summary>
[Serializable]
public class ItemStack
{
    public string id;
    public int count;

    public ItemStack(string id, int count) { this.id = id; this.count = count; }
}

/// <summary>
/// The persistent player bag (M9). A static store — like <see cref="PlayerProgress"/>
/// — backed by JSON in <see cref="SaveSystem"/>, so items carry across hub ↔ stage ↔
/// stage scene loads and reset on New Game. Pickups, crafting, quests and consumable
/// use all go through here; the bag UI / hotbar subscribe to <see cref="OnChanged"/>
/// (listen, never poll).
///
/// <para>Cross-team contract (m9-tasks §0): <see cref="TryAdd"/>. Stacks split at the
/// item's <see cref="ItemDatabase.MaxStack"/>; there is no hard slot cap (the grid
/// scrolls) so adds never soft-lock progression.</para>
/// </summary>
public static class Inventory
{
    const string SaveKey = "eco_inventory";

    /// <summary>Raised on any change (add/remove/reset). Bag UI + hotbar listen.</summary>
    public static event Action OnChanged;

    [Serializable]
    class SaveData { public List<ItemStack> stacks = new List<ItemStack>(); }

    static List<ItemStack> stacks;

    /// <summary>The current stacks, in insertion order (read-only view for UI).</summary>
    public static IReadOnlyList<ItemStack> Stacks { get { EnsureLoaded(); return stacks; } }

    // --- Queries ------------------------------------------------------------

    /// <summary>Total count of an item across all its stacks.</summary>
    public static int Count(string id)
    {
        EnsureLoaded();
        int total = 0;
        foreach (var s in stacks)
            if (s.id == id) total += s.count;
        return total;
    }

    /// <summary>True if the bag holds at least <paramref name="count"/> of the item.</summary>
    public static bool Has(string id, int count = 1) => Count(id) >= count;

    // --- Mutations ----------------------------------------------------------

    /// <summary>
    /// Add <paramref name="count"/> of <paramref name="itemId"/>, filling existing
    /// stacks before opening new ones (respecting the item's max stack). Returns
    /// true when anything was added. (Cross-team contract — see m9-tasks §0.)
    /// </summary>
    public static bool TryAdd(string itemId, int count = 1)
    {
        if (string.IsNullOrEmpty(itemId) || count <= 0) return false;
        EnsureLoaded();

        int max = ItemDatabase.MaxStack(itemId);
        int remaining = count;

        // Top up existing stacks of this id first.
        foreach (var s in stacks)
        {
            if (remaining <= 0) break;
            if (s.id != itemId || s.count >= max) continue;
            int room = max - s.count;
            int add = Mathf.Min(room, remaining);
            s.count += add;
            remaining -= add;
        }

        // Spill the rest into new stacks.
        while (remaining > 0)
        {
            int add = Mathf.Min(max, remaining);
            stacks.Add(new ItemStack(itemId, add));
            remaining -= add;
        }

        Save();
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Remove <paramref name="count"/> of an item (for crafting / consuming).
    /// Returns false and changes nothing if the bag holds fewer than that.
    /// </summary>
    public static bool TryRemove(string itemId, int count = 1)
    {
        if (string.IsNullOrEmpty(itemId) || count <= 0) return false;
        EnsureLoaded();
        if (Count(itemId) < count) return false;

        int remaining = count;
        // Drain from the last stacks first so the first slot stays stable for UI.
        for (int i = stacks.Count - 1; i >= 0 && remaining > 0; i--)
        {
            if (stacks[i].id != itemId) continue;
            int take = Mathf.Min(stacks[i].count, remaining);
            stacks[i].count -= take;
            remaining -= take;
            if (stacks[i].count <= 0) stacks.RemoveAt(i);
        }

        Save();
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>Wipe the bag (New Game — called via <see cref="SaveSystem.ResetNewGame"/>).</summary>
    public static void ResetAll()
    {
        EnsureLoaded();
        stacks.Clear();
        Save();
        OnChanged?.Invoke();
    }

    // --- Persistence --------------------------------------------------------

    static void EnsureLoaded()
    {
        if (stacks != null) return;
        stacks = SaveSystem.LoadJson<SaveData>(SaveKey).stacks ?? new List<ItemStack>();
    }

    static void Save() => SaveSystem.SaveJson(SaveKey, new SaveData { stacks = stacks });
}
