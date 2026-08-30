using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// B8: the valley's ground is a height field, and this is the field.
///
/// <para><b>Why a function and not a mesh.</b> Five separate things have to agree on where the
/// ground is, to the millimetre: the 192 tile meshes (or their shared edges show a seam), the
/// normals used to light them (recalculated per tile they would <i>also</i> disagree at the
/// seams, and lighting shows that even when geometry does not), the ~1 500 props that were all
/// authored at y = 0, the NavMesh, and the projectiles. A mesh can only answer the first; a
/// pure function answers all five and is identical in the editor and at runtime.</para>
///
/// <para><b>The relief is masked, not global.</b> Everything with a flat footprint stays on
/// level ground: the boundary walls and their 112 fence posts (the edge taper), the village,
/// the mesa, the spring, the boss grove, and the four reclamation discs — a 9 m disc lying on
/// a 9° slope buries half of itself and floats the other half. <see cref="Mask"/> is where
/// that list lives, and <see cref="GroundProfile.flat"/> is how the generator adds to it.</para>
///
/// <para>Golden rule #1 is intact: this tilts the ground Greenie walks on, it does not make
/// gravity a mechanic. Nothing jumps, nothing falls, and the <c>CharacterController</c> that
/// has always resolved slopes and steps is the thing that walks it — see the note on
/// <see cref="GroundProfile.amplitude"/> for why the amplitude is chosen against its
/// <c>slopeLimit</c> rather than by eye.</para>
/// </summary>
public static class GroundHeight
{
    /// <summary>
    /// The field the active scene is using, or null for a flat scene (the hub, Level 2, and
    /// every story scene). Published by <see cref="GroundHeightField"/> from
    /// <c>OnEnable</c> — not <c>Awake</c>, which Fast Enter Play Mode is free never to run.
    /// </summary>
    public static GroundProfile Profile { get; internal set; }

    /// <summary>Ground height at a world XZ, in metres. 0 everywhere in a flat scene.</summary>
    public static float At(float x, float z) => Profile == null ? 0f : Profile.Evaluate(x, z);

    /// <summary>Ground height under a world point (its own Y is ignored).</summary>
    public static float At(Vector3 p) => At(p.x, p.z);

    /// <summary>The ground's up vector at a world XZ. <see cref="Vector3.up"/> in a flat scene.</summary>
    public static Vector3 NormalAt(float x, float z) =>
        Profile == null ? Vector3.up : Profile.NormalAt(x, z);

    /// <summary>How far a world point sits above the ground under it, in metres.</summary>
    public static float ClearanceOf(Vector3 p) => p.y - At(p.x, p.z);

    /// <summary>
    /// Keep a flat-flying projectile the same distance above the ground it was fired at.
    ///
    /// <para>This is the projectile gate the backlog names as the prerequisite for B8, and it
    /// is deliberately the smallest answer that satisfies it. Seeds still fly <b>dead flat in
    /// XZ</b> — aim is unchanged, the spread fan is unchanged, <c>travelDir</c> is unchanged,
    /// and a shot goes exactly where the player pointed it. All that changes is that "0.60 m
    /// up" is now measured from the ground beneath the seed instead of from y = 0, so a shot
    /// fired uphill climbs with the hill and one fired downhill drops with it. On a flat
    /// scene <see cref="Profile"/> is null and this returns without touching anything, which
    /// is why Level 2 and the hub are provably unaffected.</para>
    ///
    /// <para>It steers with velocity rather than writing <c>rb.position</c>: the seed is a
    /// dynamic trigger body, and teleporting one past an enemy hurtbox is how you lose a hit.</para>
    /// </summary>
    public static void Hug(Rigidbody rb, float clearance)
    {
        if (Profile == null || rb == null) return;
        Vector3 p = rb.position;
        Vector3 v = rb.linearVelocity;
        v.y = (At(p.x, p.z) + clearance - p.y) / Time.fixedDeltaTime;
        rb.linearVelocity = v;
    }

    /// <summary>
    /// Statics survive Play here (Fast Enter Play Mode, CLAUDE.md rule 4) and this one holds a
    /// reference to a field belonging to a scene that may be long gone. A stale profile is
    /// worse than none: seeds in the flat factory would fly to a phantom hill's height.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Profile = null;
}

/// <summary>
/// The parameters of one level's ground relief. Serialized onto a <see cref="GroundHeightField"/>
/// by the level generator, which is the only thing that ever writes one.
/// </summary>
[Serializable]
public class GroundProfile
{
    /// <summary>A circle the relief is held down to y = 0 inside, feathering back out.</summary>
    [Serializable]
    public struct FlatZone
    {
        public Vector2 centre;
        [Tooltip("Dead flat within this radius.")] public float radius;
        [Tooltip("Ramps back to full relief over this many further metres.")] public float feather;
    }

    [Header("Relief")]
    [Tooltip("Nominal peak height in metres. Kept small on purpose: the ceiling is not what " +
             "looks good, it is what the CharacterController's 45 degree slopeLimit and the " +
             "NavMesh's own 45 degree max slope will still walk without a single line of new " +
             "movement code. The generator measures the steepest slope it actually produces " +
             "and logs it — see Level1Builder's ground line.")]
    public float amplitude = 1.32f;
    [Tooltip("Metres per period of the broad rolls.")] public float wavelengthA = 16f;
    [Tooltip("Metres per period of the fine detail.")] public float wavelengthB = 7.5f;
    [Tooltip("How much of the fine octave to mix in.")] public float weightB = 0.32f;
    [Tooltip("Which window of the noise the valley is cut from. Deterministic, so a rebuild " +
             "reproduces the same hills — and chosen rather than typed: Perlin is not " +
             "stationary, so the window decides whether the ground rolls both ways or just " +
             "dips. Fourteen candidates were measured; this one gives the widest range " +
             "(1.5 m) with the rises and hollows in balance.")]
    public Vector2 originA = new(188.4f, 116.2f);
    public Vector2 originB = new(40.0f, 199.9f);

    [Header("Where it is allowed to be")]
    [Tooltip("The play area's half-extents. Relief only exists inside it.")]
    public Vector2 halfExtents = new(32.5f, 24.5f);
    [Tooltip("Dead flat within this many metres of the boundary. The walls, the 112 fence " +
             "posts and everything else authored against the perimeter never move.")]
    public float edgeFlat = 4f;
    [Tooltip("Ramps up to full relief over this many further metres inward.")]
    public float edgeFeather = 5f;
    public List<FlatZone> flat = new();

    /// <summary>Ground height at a world XZ, in metres.</summary>
    public float Evaluate(float x, float z)
    {
        float m = Mask(x, z);
        if (m <= 0f) return 0f;

        float a = Mathf.PerlinNoise(originA.x + x / wavelengthA, originA.y + z / wavelengthA);
        float b = Mathf.PerlinNoise(originB.x + x / wavelengthB, originB.y + z / wavelengthB);
        // Perlin is [0, 1] and centred on 0.5; recentre so the valley dips as often as it rises.
        float h = (a - 0.5f) * 2f + (b - 0.5f) * 2f * weightB;
        return amplitude * h / (1f + weightB) * m;
    }

    /// <summary>
    /// The ground's up vector at a world XZ, by central difference.
    ///
    /// <para>Analytic rather than <c>Mesh.RecalculateNormals</c>, and that is the whole reason
    /// the tiles do not show a lighting seam: a per-tile recalculation gives an edge vertex a
    /// normal averaged over only the faces <i>on that tile</i>, so the same vertex gets two
    /// different normals from its two owners. This gives one answer per position.</para>
    /// </summary>
    public Vector3 NormalAt(float x, float z)
    {
        const float e = 0.25f;
        float dx = Evaluate(x + e, z) - Evaluate(x - e, z);
        float dz = Evaluate(x, z + e) - Evaluate(x, z - e);
        return new Vector3(-dx, 2f * e, -dz).normalized;
    }

    /// <summary>0 where the ground must stay flat, 1 where the relief is at full strength.</summary>
    public float Mask(float x, float z)
    {
        float m = Edge(halfExtents.x - Mathf.Abs(x)) * Edge(halfExtents.y - Mathf.Abs(z));
        if (m <= 0f) return 0f;

        for (int i = 0; i < flat.Count; i++)
        {
            var zone = flat[i];
            float d = Vector2.Distance(new Vector2(x, z), zone.centre);
            if (d >= zone.radius + zone.feather) continue;
            if (d <= zone.radius) return 0f;
            m *= Mathf.SmoothStep(0f, 1f, (d - zone.radius) / Mathf.Max(0.01f, zone.feather));
        }
        return m;
    }

    /// <summary><paramref name="inset"/> = metres from the boundary; 0 at the wall, 1 well inside.</summary>
    float Edge(float inset) =>
        Mathf.SmoothStep(0f, 1f, (inset - edgeFlat) / Mathf.Max(0.01f, edgeFeather));

    /// <summary>Hold the relief down in a circle. Used by the generator as it places features.</summary>
    public void Flatten(Vector2 centre, float radius, float feather = 5.5f) =>
        flat.Add(new FlatZone { centre = centre, radius = radius, feather = feather });
}
