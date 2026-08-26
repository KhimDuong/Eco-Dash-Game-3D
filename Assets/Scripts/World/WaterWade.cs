using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shallow water Greenie can walk into — the spring at the mesa's foot.
///
/// <para><b>Why this exists.</b> The spring used to be fenced off by a sphere collider sunk to
/// <c>center.y = -1.4</c>, on the theory that a cut-off sphere would block a little inside the
/// visible rim and let the player stand on the bank. The arithmetic ran the other way: a sphere
/// cut at Greenie's shins is <i>narrower</i> than at its equator, so he stopped 0.45 m short of
/// water he could see (QA C1) — and the same sphere reached <c>y = 2.00</c>, four times his own
/// 0.60 m fire height, so it silently destroyed every Seed fired across the pool (QA C2).</para>
///
/// <para>So there is no blocker any more. The pool is a trigger, exactly as
/// <see cref="ToxicMud"/> is: it borrows the same <see cref="PlayerController.EnterMud"/> /
/// <see cref="PlayerController.ExitMud"/> hook to slow the wade, and eases
/// <see cref="PlayerAnimator.SinkOffset"/> down so Greenie reads as standing <i>in</i> the water
/// rather than on it. No new movement code, no collider over the water, and golden rule #1 is
/// untouched — the ground is still flat and the dip is presentation only.</para>
///
/// <para>The slimes are kept out by a <c>NavMeshModifierVolume</c> over the same footprint
/// rather than by physics, so the water stops what walks without standing in the way of what
/// flies over it.</para>
/// </summary>
[RequireComponent(typeof(Collider))]
public class WaterWade : MonoBehaviour
{
    [Tooltip("How far the player's visual sinks while wading, in metres.")]
    [SerializeField] float sinkDepth = 0.15f;
    [Tooltip("Seconds to ease in and out of the dip.")]
    [SerializeField] float sinkDuration = 0.15f;

    /// <summary>
    /// Who is standing in the water, by collider rather than by event count. Disabling a
    /// <see cref="CharacterController"/> inside a trigger does not reliably raise
    /// <c>OnTriggerExit</c>, so a teleport or a respawn can deliver a second
    /// <c>OnTriggerEnter</c> with no exit in between — and an unbalanced pair would leave
    /// Greenie permanently slowed. A set cannot double-count.
    /// </summary>
    readonly HashSet<Collider> inside = new();

    PlayerAnimator visuals;
    float dip;

    void Reset() => GetComponent<Collider>().isTrigger = true;

    void Update()
    {
        if (visuals == null) return;

        float want = inside.Count > 0 ? sinkDepth : 0f;
        float step = sinkDuration > 0f ? sinkDepth / sinkDuration * Time.deltaTime : sinkDepth;
        dip = Mathf.MoveTowards(dip, want, step);
        visuals.SinkOffset = -dip;

        if (dip <= 0f && inside.Count == 0) { visuals.SinkOffset = 0f; visuals = null; }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!other.TryGetComponent<PlayerController>(out var player)) return;
        if (!inside.Add(other)) return;

        player.EnterMud();
        if (visuals == null) visuals = other.GetComponent<PlayerAnimator>();
    }

    void OnTriggerExit(Collider other)
    {
        if (!inside.Remove(other)) return;
        if (other.TryGetComponent<PlayerController>(out var player)) player.ExitMud();
    }

    // A scene change, or the player dying in the pool, must not leave the mesh sunk.
    void OnDisable()
    {
        if (visuals != null) visuals.SinkOffset = 0f;
        visuals = null;
        dip = 0f;
        inside.Clear();
    }
}
