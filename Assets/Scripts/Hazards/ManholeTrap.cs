using UnityEngine;

/// <summary>
/// Bẫy hố ga (manhole trap). Cycles Closed → Telegraph (the lid rattles) → Open.
/// While open it is a hazard: standing over it damages Greenie and briefly
/// <i>roots</i> him (movement suspended) so the next laser sweep or fly-bot orb
/// is more likely to land. Rooting reuses <see cref="PlayerController.ApplyKnockback"/>
/// with a zero impulse (zeroes velocity + suspends steering for a moment).
///
/// <para>3D port (Tier 1): <c>Collider2D</c> → <see cref="Collider"/>, the lid/hole
/// sprites become child objects toggled on and off, and the rattle jitters across
/// <b>XZ</b> — the ground plane — instead of XY.</para>
/// </summary>
[RequireComponent(typeof(Collider))]
public class ManholeTrap : MonoBehaviour
{
    [Header("Cycle (seconds)")]
    [SerializeField] float closedTime = 2.2f;
    [SerializeField] float telegraphTime = 0.6f;
    [SerializeField] float openTime = 1.8f;

    [Header("Effect")]
    [SerializeField] int damage = 1;
    [Tooltip("How long Greenie is rooted (movement suspended) on a landed bite.")]
    [SerializeField] float rootDuration = 0.5f;
    [Tooltip("Seconds between bites while standing in an open trap.")]
    [SerializeField] float damageInterval = 0.6f;

    [Header("Visual")]
    [Tooltip("Plate that covers the hole when closed.")]
    [SerializeField] GameObject lid;
    [Tooltip("Dark hole revealed when open.")]
    [SerializeField] GameObject hole;
    [SerializeField] float rattleAmount = 0.05f;

    [Header("Audio")]
    [SerializeField] AudioClip openSfx;

    enum Phase { Closed, Telegraph, Open }
    Phase phase = Phase.Closed;
    float phaseTimer;
    float nextDamageTime;
    Vector3 lidBase;
    bool playerInside;
    PlayerHealth playerHp;
    PlayerController playerCtrl;

    /// <summary>True while the hole is open and biting (drives the probe).</summary>
    public bool IsOpen => phase == Phase.Open;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (lid != null) lidBase = lid.transform.localPosition;
        phaseTimer = closedTime;
        ApplyVisual();
    }

    void Update()
    {
        phaseTimer -= Time.deltaTime;

        if (phase == Phase.Telegraph && lid != null)
        {
            Vector2 r = Random.insideUnitCircle * rattleAmount;
            lid.transform.localPosition = lidBase + new Vector3(r.x, 0f, r.y);
        }

        if (phaseTimer <= 0f) Advance();

        if (phase == Phase.Open && playerInside && Time.time >= nextDamageTime) Bite();
    }

    void Advance()
    {
        switch (phase)
        {
            case Phase.Closed:
                phase = Phase.Telegraph; phaseTimer = telegraphTime; break;
            case Phase.Telegraph:
                phase = Phase.Open; phaseTimer = openTime;
                if (lid != null) lid.transform.localPosition = lidBase;
                if (openSfx != null) Sfx.Play(openSfx, transform.position);
                break;
            case Phase.Open:
                phase = Phase.Closed; phaseTimer = closedTime; break;
        }
        ApplyVisual();
        if (phase == Phase.Open && playerInside) Bite(); // caught standing on it as it opens
    }

    void Bite()
    {
        nextDamageTime = Time.time + damageInterval;
        if (playerHp != null && playerHp.TakeDamage(damage, transform.position, 0f) && playerCtrl != null)
            playerCtrl.ApplyKnockback(Vector3.zero, rootDuration);
    }

    void ApplyVisual()
    {
        if (lid != null) lid.SetActive(phase != Phase.Open);
        if (hole != null) hole.SetActive(phase == Phase.Open);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = true;
        other.TryGetComponent(out playerHp);
        other.TryGetComponent(out playerCtrl);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInside = false;
    }
}
