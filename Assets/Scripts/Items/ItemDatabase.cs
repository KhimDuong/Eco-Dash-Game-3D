using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central lookup from a string item <c>id</c> (the cross-team contract) to its
/// <see cref="ItemDef"/>. It loads every authored <see cref="ItemDef"/> asset from
/// any <c>Resources/Items</c> folder, then fills in a built-in fallback for any
/// contract id that has no asset yet — so <see cref="Inventory"/>, crafting and
/// quests all work with plain ids before Khang's art/ScriptableObjects land. When a
/// real asset with the same id is added it simply overrides the fallback.
///
/// <para>A static store like <see cref="PlayerProgress"/>; loads lazily on first
/// access, no scene wiring required.</para>
/// </summary>
public static class ItemDatabase
{
    static Dictionary<string, ItemDef> byId;

    /// <summary>
    /// Drop the cache when Play is pressed — an editor-only fix for an editor-only trap.
    ///
    /// <para>The fallback defs are <see cref="ScriptableObject"/>s created at runtime, and Unity
    /// destroys those when play mode ends. The project runs with Fast Enter Play Mode, so the
    /// domain is not reloaded and <see cref="byId"/> survives into the next session still holding
    /// every one of them — destroyed. <see cref="Get"/> then hands back an object that compares
    /// equal to null, so from the <b>second</b> Play onwards every id looks unknown: consumables
    /// refuse to be used, display names fall back to raw ids, and nothing logs a word about it.
    /// A build never sees this (each run is a fresh process), which is exactly what makes it
    /// expensive — it only ever bites in the editor, where all the testing happens.</para>
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => byId = null;

    /// <summary>Resolve an id to its def, or null if the id is unknown.</summary>
    public static ItemDef Get(string id)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(id)) return null;
        return byId.TryGetValue(id, out var def) ? def : null;
    }

    /// <summary>Display name for an id, falling back to the raw id if unknown.</summary>
    public static string DisplayName(string id)
    {
        var def = Get(id);
        return def != null ? def.displayName : id;
    }

    /// <summary>Max stack for an id (99 when unknown, so adds never silently fail).</summary>
    public static int MaxStack(string id)
    {
        var def = Get(id);
        return def != null ? def.maxStack : 99;
    }

    static void EnsureLoaded()
    {
        if (byId != null) return;
        byId = new Dictionary<string, ItemDef>();

        // 1) Authored assets win — load all ItemDefs from any Resources/Items folder.
        foreach (var def in Resources.LoadAll<ItemDef>("Items"))
        {
            if (def != null && !string.IsNullOrEmpty(def.id))
                byId[def.id] = def;
        }

        // 2) Fill gaps with built-in fallbacks for every contract id (m9-tasks §0,
        //    game-design §4.7.2). Real assets above take precedence.
        foreach (var def in BuiltInCatalog())
            if (!byId.ContainsKey(def.id))
                byId[def.id] = def;
    }

    // Built-in defaults so the systems work before any asset exists. Names/effects
    // come from game-design §4.7.2–4.7.3. Icons stay null until Khang's art lands.
    static IEnumerable<ItemDef> BuiltInCatalog()
    {
        // --- Materials (stack to 99) ---
        yield return ItemDef.CreateRuntime("bottle", "Chai Nhựa", ItemCategory.Material, 99);
        yield return ItemDef.CreateRuntime("scrap", "Mảnh Kim Loại", ItemCategory.Material, 99);
        yield return ItemDef.CreateRuntime("herb", "Lá Thuốc", ItemCategory.Material, 99);
        yield return ItemDef.CreateRuntime("energy_shard", "Tinh Thể Năng Lượng", ItemCategory.Material, 99,
            description: "Tinh thể năng lượng hiếm — nguyên liệu chế tạo cao cấp.");

        // --- Consumables (used from the hotbar) ---
        yield return ItemDef.CreateRuntime("spring_water", "Bình Nước Suối", ItemCategory.Consumable, 99,
            ItemUseEffect.Heal, effectAmount: 2f, description: "Hồi 2 máu.");
        yield return ItemDef.CreateRuntime("energy_drink", "Nước Tăng Lực Mầm Xanh", ItemCategory.Consumable, 99,
            ItemUseEffect.SpeedBoost, effectAmount: 0.5f, effectDuration: 8f, description: "Tăng 50% tốc độ trong 8 giây.");
        yield return ItemDef.CreateRuntime("sprout_shield", "Lá Chắn Mầm", ItemCategory.Consumable, 99,
            ItemUseEffect.Shield, effectDuration: 6f, description: "Khiên tạm thời chặn sát thương.");
        yield return ItemDef.CreateRuntime("seed_bomb", "Bom Hạt Giống", ItemCategory.Consumable, 99,
            ItemUseEffect.SeedBomb, description: "Ném ra vùng nổ dọn rác và gây sát thương.");
        yield return ItemDef.CreateRuntime("large_heal", "Bình Hồi Phục Lớn", ItemCategory.Consumable, 99,
            ItemUseEffect.FullHeal, description: "Hồi đầy máu.");

        // --- Key / quest items (kept out of the main grid; still counted) ---
        yield return ItemDef.CreateRuntime("antidote", "Thuốc Giải Mầm Xanh", ItemCategory.KeyQuest, 1,
            description: "Thuốc giải cứu Tí khỏi nhiễm độc.");
        yield return ItemDef.CreateRuntime("keycard", "Thẻ Từ", ItemCategory.KeyQuest, 99,
            description: "Mở cửa an ninh trong nhà máy.");
        yield return ItemDef.CreateRuntime("portal_shard", "Mảnh Cổng", ItemCategory.KeyQuest, 99,
            description: "Mảnh năng lượng để sửa và kích hoạt cổng dịch chuyển.");
    }
}
