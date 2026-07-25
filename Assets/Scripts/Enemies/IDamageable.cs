/// <summary>
/// Implemented by anything that can be hurt by a damage source
/// (e.g. the player's Seed projectile). Keeps attackers decoupled from
/// concrete enemy types. See .claude/docs/architecture.md.
/// </summary>
public interface IDamageable
{
    void TakeDamage(int amount);
}
