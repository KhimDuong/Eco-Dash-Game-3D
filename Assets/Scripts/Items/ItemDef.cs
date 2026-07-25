using UnityEngine;

/// <summary>Where an item lives in the bag and how it behaves.</summary>
public enum ItemCategory
{
    /// <summary>Used from the hotbar for an effect (heal, buff, throw).</summary>
    Consumable,
    /// <summary>Crafting/quest resource; stacks high (drops from trash/enemies).</summary>
    Material,
    /// <summary>Key / quest item; shown in a separate strip, never the main grid.</summary>
    KeyQuest,
}

/// <summary>What happens when a consumable is used from the hotbar.</summary>
public enum ItemUseEffect
{
    None,
    /// <summary>Restore <see cref="ItemDef.effectAmount"/> HP.</summary>
    Heal,
    /// <summary>Multiply move speed by (1 + amount) for <see cref="ItemDef.effectDuration"/>s.</summary>
    SpeedBoost,
    /// <summary>Grant a temporary damage-absorbing shield for <see cref="ItemDef.effectDuration"/>s.</summary>
    Shield,
    /// <summary>Throw a Seed Bomb — AoE that clears trash and damages enemies.</summary>
    SeedBomb,
    /// <summary>Restore full HP.</summary>
    FullHeal,
}

/// <summary>
/// Authoring data for one item type (M9 inventory): its stable string <see cref="id"/>
/// (the cross-team contract — see <c>m9-tasks.md §0</c>), Vietnamese display name,
/// icon, category, stack limit, and (for consumables) its use effect. Pure data —
/// not a MonoBehaviour. Authored as a ScriptableObject asset under a
/// <c>Resources/Items</c> folder so <see cref="ItemDatabase"/> can load it by id;
/// until Khang's assets land, <see cref="ItemDatabase"/> supplies a built-in
/// fallback for every contract id so the systems work without any asset present.
/// </summary>
[CreateAssetMenu(menuName = "Eco-Dash/Item Definition", fileName = "item_")]
public class ItemDef : ScriptableObject
{
    [Tooltip("Stable lowercase id — the cross-team contract (e.g. \"bottle\"). Icons are named icon_<id>.")]
    public string id;

    [Tooltip("Vietnamese name shown in the bag, e.g. \"Chai Nhựa\".")]
    public string displayName;

    [TextArea(2, 4)]
    [Tooltip("Vietnamese flavour / tooltip text.")]
    public string description;

    [Tooltip("Slot icon. Khang names these icon_<id>. May be null while art is pending.")]
    public Sprite icon;

    public ItemCategory category = ItemCategory.Material;

    [Tooltip("Max items per stack (materials stack high; key items low).")]
    public int maxStack = 99;

    [Header("Consumable effect (ignored for non-consumables)")]
    public ItemUseEffect useEffect = ItemUseEffect.None;

    [Tooltip("Effect magnitude: HP for Heal, fraction for SpeedBoost (0.5 = +50%).")]
    public float effectAmount;

    [Tooltip("Effect duration in seconds (SpeedBoost / Shield).")]
    public float effectDuration;

    /// <summary>
    /// Build an in-memory <see cref="ItemDef"/> at runtime (no asset on disk). Used
    /// by <see cref="ItemDatabase"/> to back contract ids that have no authored
    /// asset yet, so gameplay/crafting/quests work before Khang's art arrives.
    /// </summary>
    public static ItemDef CreateRuntime(string id, string displayName, ItemCategory category,
        int maxStack, ItemUseEffect useEffect = ItemUseEffect.None,
        float effectAmount = 0f, float effectDuration = 0f, string description = "")
    {
        var def = CreateInstance<ItemDef>();
        def.id = id;
        def.displayName = displayName;
        def.description = description;
        def.category = category;
        def.maxStack = Mathf.Max(1, maxStack);
        def.useEffect = useEffect;
        def.effectAmount = effectAmount;
        def.effectDuration = effectDuration;
        return def;
    }
}
