using UnityEngine;

/// <summary>
/// Tia laser quét (sweeping toxic laser). A wall-mounted emitter whose beam
/// rotates back and forth across an arc, on a timed Off → Telegraph → Active
/// cycle. While Active the beam damages the player on contact (with knockback),
/// detected by an oriented overlap box along the beam. Top-down hazard: read the
/// telegraph and slip through the gap while it's off.
///
/// <para><b>Tier 2 (redesigned for 3D).</b> Timings, arc, damage and knockback are
/// the 2D numbers. What changed:</para>
/// <list type="bullet">
/// <item>The emitter <b>sweeps around Y</b>, not Z. In 2D "rotate the sprite in the
/// screen plane" and "sweep across the floor" were the same operation; in 3D they are
/// different axes, and Y is the one that sweeps the ground.</item>
/// <item>The beam is a stretched mesh, so the 2D alpha ramp (Off = invisible) becomes
/// a renderer toggle plus a colour swap: opaque greybox meshes have no alpha to fade,
/// and "the beam is off" has to be unmistakable.</item>
/// <item>Detection is <see cref="Physics.OverlapBox"/> filtered to the Player layer.
/// The 2D version scanned <em>everything</em> the beam touched and discarded the
/// non-players; in a factory full of crates that is a lot of wasted overlap.</item>
/// </list>
/// </summary>
public class SweepingLaser : MonoBehaviour
{
    [Header("Beam (child stretched along +X from the emitter)")]
    [SerializeField] Transform beam;
    [SerializeField] Renderer beamRenderer;
    [SerializeField] float beamLength = 4f;
    [SerializeField] float beamWidth = 0.35f;
    [Tooltip("How tall the damaging volume is. Greenie's capsule is 1.15 m.")]
    [SerializeField] float beamHeight = 1.2f;

    [Header("Sweep")]
    [SerializeField] float minAngle = -50f;
    [SerializeField] float maxAngle = 50f;
    [Tooltip("Ping-pong sweeps per second.")]
    [SerializeField] float sweepSpeed = 0.45f;

    [Header("Cycle (seconds)")]
    [SerializeField] float offTime = 1.6f;
    [SerializeField] float telegraphTime = 0.7f;
    [SerializeField] float activeTime = 1.4f;

    [Header("Damage")]
    [SerializeField] int damage = 1;
    [SerializeField] float knockback = 8f;
    [Tooltip("Layers the beam can burn. Defaults to Player when left empty.")]
    [SerializeField] LayerMask hitMask;

    [Header("Look")]
    [SerializeField] Color telegraphColor = new Color(1f, 0.45f, 0.45f, 0.35f);
    [SerializeField] Color activeColor = new Color(0.6f, 1f, 0.4f, 0.95f); // toxic green
    [SerializeField] AudioClip fireSfx;

    enum Phase { Off, Telegraph, Active }
    Phase phase = Phase.Off;
    float phaseTimer;
    readonly Collider[] hits = new Collider[8];

    /// <summary>True while the beam is live and burning (drives the probe).</summary>
    public bool IsFiring => phase == Phase.Active;

    void Awake()
    {
        phaseTimer = offTime;
        if (beamRenderer == null && beam != null) beamRenderer = beam.GetComponentInChildren<Renderer>();
        if (hitMask == 0) hitMask = 1 << LayerMask.NameToLayer("Player");
        ConfigureBeam();
        ApplyPhaseVisual();
    }

    void ConfigureBeam()
    {
        if (beam == null) return;
        beam.localPosition = new Vector3(beamLength * 0.5f, 0f, 0f);
        beam.localScale = new Vector3(beamLength, beamHeight, beamWidth);
    }

    void Update()
    {
        float t = Mathf.PingPong(Time.time * sweepSpeed, 1f);
        transform.localRotation = Quaternion.Euler(0f, Mathf.Lerp(minAngle, maxAngle, t), 0f);

        phaseTimer -= Time.deltaTime;
        if (phaseTimer <= 0f) Advance();
    }

    void Advance()
    {
        switch (phase)
        {
            case Phase.Off:
                phase = Phase.Telegraph; phaseTimer = telegraphTime; break;
            case Phase.Telegraph:
                phase = Phase.Active; phaseTimer = activeTime;
                if (fireSfx != null) AudioSource.PlayClipAtPoint(fireSfx, transform.position);
                break;
            case Phase.Active:
                phase = Phase.Off; phaseTimer = offTime; break;
        }
        ApplyPhaseVisual();
    }

    void ApplyPhaseVisual()
    {
        if (beamRenderer == null) return;
        // Off has to *read* as off. The 2D beam faded to alpha 0; a greybox mesh can't,
        // so the renderer goes away entirely and comes back for the telegraph.
        beamRenderer.enabled = phase != Phase.Off;
        if (phase != Phase.Off)
            MaterialTint.Apply(beamRenderer, phase == Phase.Active ? activeColor : telegraphColor);
    }

    void FixedUpdate()
    {
        if (phase != Phase.Active || beam == null) return;

        Vector3 half = new Vector3(beamLength, beamHeight, beamWidth) * 0.5f;
        int n = Physics.OverlapBoxNonAlloc(beam.position, half, hits, beam.rotation, hitMask,
                                           QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
            if (hits[i].CompareTag("Player") && hits[i].TryGetComponent<PlayerHealth>(out var hp))
                hp.TakeDamage(damage, transform.position, knockback);
    }

    void OnDrawGizmosSelected()
    {
        if (beam == null) return;
        Gizmos.color = new Color(0.6f, 1f, 0.4f, 0.35f);
        Gizmos.matrix = Matrix4x4.TRS(beam.position, beam.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(beamLength, beamHeight, beamWidth));
    }
}
