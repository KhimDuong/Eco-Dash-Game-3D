using System;
using UnityEngine;

/// <summary>
/// Persistent player progression (roadmap M4): the recycling-trash currency and
/// the three permanent upgrade tiers (max HP, move speed, Seed spread), backed by
/// <see cref="PlayerPrefs"/> so they survive scene loads and app restarts.
///
/// This is the single source of truth for trash: <see cref="GameManager.AddTrash"/>
/// routes into <see cref="AddTrash"/>, and Mr. Bear's shop spends it via
/// <see cref="TryBuy"/>. A static store (not a scene object) so any scene can read
/// it without wiring; it loads lazily on first access.
/// </summary>
public static class PlayerProgress
{
    public enum Upgrade { MaxHp, Speed, Spread }

    /// <summary>Highest level each upgrade can reach (levels 0..MaxTier).</summary>
    public const int MaxTier = 3;

    // Tunables (centralised here since a static store has no Inspector).
    const int HpPerTier = 2;            // +2 max HP per tier
    const float SpeedPerTier = 0.12f;   // +12% move speed per tier
    static readonly int[] TierCost = { 10, 18, 30 }; // trash to buy level 1, 2, 3

    const string TrashKey = "eco_trash";
    const string MaxHpKey = "eco_lvl_maxhp";
    const string SpeedKey = "eco_lvl_speed";
    const string SpreadKey = "eco_lvl_spread";

    /// <summary>Raised whenever trash or any upgrade tier changes (shop/HUD listen).</summary>
    public static event Action OnChanged;

    static bool loaded;
    static int trash, maxHpLevel, speedLevel, spreadLevel;

    public static int Trash { get { EnsureLoaded(); return trash; } }

    public static int GetLevel(Upgrade u)
    {
        EnsureLoaded();
        return u switch
        {
            Upgrade.MaxHp => maxHpLevel,
            Upgrade.Speed => speedLevel,
            _ => spreadLevel,
        };
    }

    // --- Derived bonuses (read by the player scripts at spawn) --------------

    /// <summary>Extra max HP from the upgrade (added on top of the base value).</summary>
    public static int MaxHpBonus => GetLevel(Upgrade.MaxHp) * HpPerTier;

    /// <summary>Move-speed multiplier (1.0 at tier 0).</summary>
    public static float SpeedMultiplier => 1f + GetLevel(Upgrade.Speed) * SpeedPerTier;

    /// <summary>Seeds fired per shot: 1 / 3 / 5 / 7 by spread tier.</summary>
    public static int SeedCount => 1 + 2 * GetLevel(Upgrade.Spread);

    // --- Shop economy -------------------------------------------------------

    /// <summary>Trash cost to buy the next tier, or -1 if already maxed.</summary>
    public static int CostFor(Upgrade u)
    {
        int lvl = GetLevel(u);
        return lvl >= MaxTier ? -1 : TierCost[lvl];
    }

    public static bool IsMaxed(Upgrade u) => GetLevel(u) >= MaxTier;

    public static bool CanAfford(Upgrade u)
    {
        int cost = CostFor(u);
        return cost >= 0 && Trash >= cost;
    }

    /// <summary>Spend trash to raise an upgrade one tier. Returns false if maxed or too poor.</summary>
    public static bool TryBuy(Upgrade u)
    {
        if (!CanAfford(u)) return false;
        trash -= CostFor(u);
        switch (u)
        {
            case Upgrade.MaxHp: maxHpLevel++; break;
            case Upgrade.Speed: speedLevel++; break;
            default: spreadLevel++; break;
        }
        Save();
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>Bank recycled trash (called by <see cref="GameManager.AddTrash"/>).</summary>
    public static void AddTrash(int amount)
    {
        if (amount == 0) return;
        EnsureLoaded();
        trash = Mathf.Max(0, trash + amount);
        Save();
        OnChanged?.Invoke();
    }

    /// <summary>Wipe all progress (debug / new game).</summary>
    public static void ResetAll()
    {
        trash = maxHpLevel = speedLevel = spreadLevel = 0;
        Save();
        OnChanged?.Invoke();
    }

    // --- Persistence --------------------------------------------------------

    static void EnsureLoaded()
    {
        if (loaded) return;
        trash = PlayerPrefs.GetInt(TrashKey, 0);
        maxHpLevel = Mathf.Clamp(PlayerPrefs.GetInt(MaxHpKey, 0), 0, MaxTier);
        speedLevel = Mathf.Clamp(PlayerPrefs.GetInt(SpeedKey, 0), 0, MaxTier);
        spreadLevel = Mathf.Clamp(PlayerPrefs.GetInt(SpreadKey, 0), 0, MaxTier);
        loaded = true;
    }

    static void Save()
    {
        PlayerPrefs.SetInt(TrashKey, trash);
        PlayerPrefs.SetInt(MaxHpKey, maxHpLevel);
        PlayerPrefs.SetInt(SpeedKey, speedLevel);
        PlayerPrefs.SetInt(SpreadKey, spreadLevel);
        PlayerPrefs.Save();
    }
}
