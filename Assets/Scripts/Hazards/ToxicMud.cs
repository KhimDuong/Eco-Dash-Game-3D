using UnityEngine;

/// <summary>
/// Bùn lầy hóa chất (Toxic Mud) — a purple chemical pool. While the player is
/// inside this trigger zone, their move speed is reduced (handled by
/// PlayerController.EnterMud/ExitMud). Place as a trigger collider over the pool.
///
/// <para>3D port: `Collider2D` → `Collider`; the pool is a flat box lying on the
/// ground plane, so it's the XZ footprint that matters now.</para>
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
