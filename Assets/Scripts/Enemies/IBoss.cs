using System;

/// <summary>
/// What a boss has to expose for the shared HP bar to drive itself (C3).
///
/// <para>The 2D build never needed this: <c>BossHealthBar</c> held a hard reference to
/// <c>MegaSmogBoss</c>, and <c>SlimeKing</c> raised the same three events purely so a
/// second, hand-wired bar <em>could</em> subscribe. In 3D both bosses use one bar, so the
/// three events and the two counters become an actual contract. Nothing else in the game
/// depends on it — an <see cref="IBoss"/> is still an ordinary
/// <see cref="IDamageable"/> as far as Greenie's Seeds are concerned.</para>
///
/// <para>Implementers raise <see cref="OnEngaged"/> exactly once, when the fight starts,
/// and <see cref="OnDefeated"/> exactly once, on death.</para>
/// </summary>
public interface IBoss
{
    /// <summary>Vietnamese name shown above the bar, e.g. "Slime Chúa".</summary>
    string DisplayName { get; }

    int CurrentHealth { get; }
    int MaxHealth { get; }

    /// <summary>True between <see cref="OnEngaged"/> and <see cref="OnDefeated"/>.</summary>
    bool IsEngaged { get; }

    /// <summary>The player has woken the boss — show the bar.</summary>
    event Action OnEngaged;

    /// <summary>(current, max) on every health change.</summary>
    event Action<int, int> OnHealthChanged;

    /// <summary>The boss is down — hide the bar.</summary>
    event Action OnDefeated;
}
