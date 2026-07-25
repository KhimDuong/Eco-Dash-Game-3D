/// <summary>
/// Optional companion to <see cref="IDamageable"/>: something that can be shoved
/// by a hit (a Seed impact, an explosion). Kept separate so damage and knockback
/// stay decoupled — an attacker applies knockback only if the target opts in.
/// </summary>
public interface IKnockbackable
{
    /// <summary>
    /// Shove this object with a world-space impulse for <paramref name="duration"/> seconds.
    /// 3D contract: the impulse is a Vector3, and implementers should keep it on the XZ
    /// ground plane — knockback never launches anything into the air in Eco-Dash.
    /// </summary>
    void ApplyKnockback(UnityEngine.Vector3 impulse, float duration);
}
