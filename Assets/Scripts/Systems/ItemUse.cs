using UnityEngine;

/// <summary>
/// Applies a consumable's use-effect to the player (M9, K2/K3). The bag UI and the
/// hotbar call <see cref="Use"/>: it validates the item is a usable consumable,
/// applies the effect against the player in the scene, and only then consumes one
/// from the <see cref="Inventory"/>. Effects reuse the existing pickup hooks
/// (<see cref="PlayerHealth.Heal"/>, <see cref="PlayerController.ApplySpeedBoost"/>,
/// the new <see cref="PlayerHealth.ActivateShield"/>).
/// </summary>
public static class ItemUse
{
    const float SeedBombRadius = 2.5f;
    const int SeedBombDamage = 3;

    /// <summary>
    /// Use one <paramref name="itemId"/> from the bag. Returns true only if it was a
    /// usable consumable that actually applied (and was consumed). Returns false —
    /// consuming nothing — for non-consumables, an empty bag, no player present, or
    /// an effect that would be wasted (e.g. healing at full HP).
    /// </summary>
    public static bool Use(string itemId)
    {
        var def = ItemDatabase.Get(itemId);
        if (def == null || def.category != ItemCategory.Consumable || def.useEffect == ItemUseEffect.None)
            return false;
        if (!Inventory.Has(itemId)) return false;

        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo == null) return false;

        if (!Apply(def, playerGo)) return false;   // may decline (e.g. full HP)
        Inventory.TryRemove(itemId, 1);
        return true;
    }

    static bool Apply(ItemDef def, GameObject player)
    {
        switch (def.useEffect)
        {
            case ItemUseEffect.Heal:
            {
                var hp = player.GetComponent<PlayerHealth>();
                if (hp == null || hp.CurrentHealth >= hp.MaxHealth) return false;
                hp.Heal(Mathf.RoundToInt(def.effectAmount));
                return true;
            }
            case ItemUseEffect.FullHeal:
            {
                var hp = player.GetComponent<PlayerHealth>();
                if (hp == null || hp.CurrentHealth >= hp.MaxHealth) return false;
                hp.Heal(hp.MaxHealth);
                return true;
            }
            case ItemUseEffect.SpeedBoost:
            {
                var pc = player.GetComponent<PlayerController>();
                if (pc == null) return false;
                pc.ApplySpeedBoost(1f + def.effectAmount, def.effectDuration);
                return true;
            }
            case ItemUseEffect.Shield:
            {
                var hp = player.GetComponent<PlayerHealth>();
                if (hp == null) return false;
                hp.ActivateShield(def.effectDuration);
                return true;
            }
            case ItemUseEffect.SeedBomb:
                return DetonateSeedBomb(player.transform.position);
            default:
                return false;
        }
    }

    // AoE that damages nearby enemies. The trash-clearing half of the Seed Bomb
    // (game-design §4.7.2) is wired to the cleaning loop in A5/C4 once that
    // component lands — see m9-tasks K3/A5.
    static bool DetonateSeedBomb(Vector3 center)
    {
        var hits = Physics.OverlapSphere(center, SeedBombRadius, ~0, QueryTriggerInteraction.Collide);
        foreach (var h in hits)
            if (h.TryGetComponent<IDamageable>(out var dmg))
                dmg.TakeDamage(SeedBombDamage);
        return true;
    }
}
