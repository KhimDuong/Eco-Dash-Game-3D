using UnityEngine;

/// <summary>
/// Bùn lầy hóa chất (Toxic Mud) — a purple chemical pool. While the player is
/// inside this trigger zone, their move speed is reduced (handled by
/// PlayerController.EnterMud/ExitMud). Place as a trigger collider over the pool.
///
/// <para>3D port: `Collider2D` → `Collider`; the pool is a flat box lying on the
/// ground plane, so it's the XZ footprint that matters now.</para>
///
/// <para><b>Not in the game right now (2026-08-31, PO call).</b> The script and the prefab are
/// kept, but <c>Level1Builder.ToxicMudEnabled</c> is <c>false</c>, so no scene instances it —
/// Level 1 was the only one that ever did. The reason is the art: QA had already recorded that
/// the pools "read as flat pale sheets from eye height", which was a footnote while the ¾ camera
/// was the default and is the first thing a player sees now that first person is. Flip the switch
/// and rebuild Level 1 to bring it back.</para>
///
/// <para><b>Fix this before it comes back.</b> <c>EnterMud</c>/<c>ExitMud</c> are counted here by
/// <i>event</i>, and an unbalanced pair leaves Greenie permanently at half speed with no way to
/// clear it. Disabling a <see cref="CharacterController"/> inside a trigger does not reliably
/// raise <c>OnTriggerExit</c>, so a teleport or a respawn out of a pool does exactly that.
/// <see cref="WaterWade"/> hit this first and answered it with a <c>HashSet&lt;Collider&gt;</c>
/// plus an <c>OnDisable</c> that clears it; this script needs the same treatment and has only
/// been safe because nothing teleports into a mud pool. See
/// <c>architecture.md § the spring is a wade volume</c>.</para>
/// </summary>
[RequireComponent(typeof(Collider))]
public class ToxicMud : MonoBehaviour
{
    void Reset()
    {
        // Make sure designers get a trigger collider by default.
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<PlayerController>(out var player))
            player.EnterMud();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<PlayerController>(out var player))
            player.ExitMud();
    }
}
