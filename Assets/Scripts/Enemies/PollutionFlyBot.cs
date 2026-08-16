using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pollution Fly-Bot (Phi Cơ Ô Nhiễm) — Level 2's advanced enemy. Hovers and
/// patrols around its spawn; when Greenie enters its sight (distance + clear
/// line-of-sight) it switches to Chase, holding a preferred range while firing
/// Smog Orbs on a cadence, and gives up if Greenie escapes past a larger radius
/// or breaks line of sight. Dies to Seeds (<see cref="IDamageable"/>), can be
/// shoved (<see cref="IKnockbackable"/>), and drops trash on death.
///
/// <para><b>Tier 2 (redesigned for 3D).</b> Every stat, drop and damage number is
/// the 2D one. Three things are genuinely different:</para>
/// <list type="bullet">
/// <item><b>It actually flies.</b> The 2D "hover" was cosmetic — the bot slid
/// around the same plane as everything else. Here Y is a real axis: a downward
/// probe finds whatever is under the bot and it holds <see cref="hoverHeight"/>
/// above <em>that</em>, so it rises over crates and follows the factory floor
/// instead of clipping through it.</item>
/// <item><b>Line of sight is the shot line.</b> The 2D raycast ran centre-to-centre;
/// here it runs from the fire point along the exact flat path the orb will take, so
/// the bot can never "see" you through a crate its own orbs would splash against.</item>
/// <item><b>Contact damage is a distance test</b>, not <c>OnCollisionStay</c> — the
/// bot floats above Greenie's capsule, so the two colliders never actually meet, and
/// under a fixed ¾ camera an overlap on XZ is what reads as "it's on top of you".</item>
/// </list>
///
/// <para>Steering is still velocity on a dynamic <see cref="Rigidbody"/> rather than
/// a NavMeshAgent, as in 2D: the bot flies over the low clutter a ground agent has to
/// walk around, and Level 2's walls stop it through ordinary physics.</para>
///
/// <para><b>The prefab carries two colliders and both are load-bearing.</b> The solid
/// sphere rides with the body and is what walls push against, so it must stay high or
/// the bot stops clearing crates. The trigger capsule hangs from the body down to just
/// above the floor, because <see cref="SeedProjectile"/> flies flat at Greenie's fire
/// point (y ≈ 0.6) — with only the sphere, every shot he can take passes a clear metre
/// underneath and the bot is literally unkillable. Flight is presentation; what you can
/// hit stays a question of XZ. Keep both, and keep the hurtbox on the <b>root</b>: the
/// projectiles resolve <see cref="IDamageable"/> off the collider they hit.</para>
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PollutionFlyBot : MonoBehaviour, IDamageable, IKnockbackable
{
    [Header("Stats")]
    [SerializeField] int maxHealth = 3;
    [SerializeField] int contactDamage = 1;
    [SerializeField] int trashDropped = 2;
    [SerializeField] float contactKnockback = 7f;

    [Header("Detection")]
    [SerializeField] float sightRadius = 6f;
    [SerializeField] float giveUpRadius = 9f;
    [Tooltip("Layers that block line of sight. Defaults to Obstacle when left empty.")]
    [SerializeField] LayerMask sightBlockers;
    [Tooltip("Seconds a Seed hit keeps the bot hunting even with sight broken or from " +
             "beyond giveUpRadius. Without it, sniping from range flips it to Chase and " +
             "straight back to Patrol on the very next step (same trap as PlasticSlime).")]
    [SerializeField] float provokeDuration = 4f;

    [Header("Movement")]
    [SerializeField] float patrolSpeed = 1.5f;
    [SerializeField] float chaseSpeed = 2.8f;
    [Tooltip("Distance the bot tries to hold from Greenie while shooting.")]
    [SerializeField] float preferredRange = 4f;
    [SerializeField] float wanderRadius = 2.5f;
    [SerializeField] float minRepathTime = 1.5f;
    [SerializeField] float maxRepathTime = 3f;

    [Header("Hover (new in 3D)")]
    [Tooltip("Metres held above whatever is directly below — floor, crate or catwalk.")]
    [SerializeField] float hoverHeight = 1.5f;
    [Tooltip("Layers the downward probe treats as 'ground'. Defaults to Ground + Obstacle.")]
    [SerializeField] LayerMask groundMask;
    [Tooltip("How hard the bot corrects its height (1/s). Low values make it drift lazily.")]
    [SerializeField] float hoverStiffness = 4f;
    [SerializeField] float maxClimbSpeed = 4f;

    [Header("Attack")]
    [SerializeField] GameObject smogOrbPrefab;
    [Tooltip("Nozzle under the bot's belly. Orbs fly flat from here, so it must sit at " +
             "roughly Greenie's chest height or every shot sails over his head.")]
    [SerializeField] Transform firePoint;
    [SerializeField] float fireCooldown = 1.6f;

    [Header("Contact")]
    [Tooltip("Centre-to-centre distance on the ground plane that counts as touching.")]
    [SerializeField] float contactRange = 1f;

    [Header("Feedback (optional)")]
    [SerializeField] HitFlash flash;
    [Tooltip("Child holding the mesh. Bobs and turns independently of the physics body.")]
    [SerializeField] Transform visual;
    [SerializeField] float bobAmplitude = 0.09f;
    [SerializeField] float bobSpeed = 1.6f;
    [SerializeField] float turnSpeed = 540f;
    [SerializeField] AudioClip shootSfx;
    [SerializeField] AudioClip deathSfx;

    // Fallback for the death poof only; the art pass owns the colour it normally reads.
    static readonly Color DeathTint = new Color(0.55f, 0.58f, 0.62f);

    enum State { Patrol, Chase }
    State state = State.Patrol;

    Rigidbody rb;
    Transform player;
    PlayerHealth playerHealth;
    Vector3 spawn, targetPoint;
    Vector3 baseVisualPos;
    Vector3 facing = Vector3.forward;
    float repathTimer, nextFireTime, knockbackUntil, provokedUntil, nextPlayerScan, bobPhase;
    int health;
    bool dead;

    // Same reason as PlasticSlime: SceneProgress ids an object by name + position, so
    // anything that moves must bank the id it had where it *spawned*, not where it fell.
    string sceneName, spawnId;

    /// <summary>True while the bot is hunting the player rather than patrolling.</summary>
    public bool IsChasing => state == State.Chase;
    public int Health => health;

    void Awake()
    {
        // Already killed on a previous visit? Stay dead (M9 save persistence).
        sceneName = SceneManager.GetActiveScene().name;
        spawnId = SceneProgress.IdFor(gameObject);
        if (SceneProgress.IsConsumed(sceneName, spawnId)) { Destroy(gameObject); return; }

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;      // the hover controller owns Y entirely
        rb.freezeRotation = true;   // the visual child does the turning
        health = maxHealth;

        if (flash == null) flash = GetComponent<HitFlash>();
        if (firePoint == null) firePoint = transform;
        if (visual != null) baseVisualPos = visual.localPosition;
        if (sightBlockers == 0) sightBlockers = 1 << LayerMask.NameToLayer("Obstacle");
        if (groundMask == 0) groundMask = (1 << LayerMask.NameToLayer("Ground")) |
                                          (1 << LayerMask.NameToLayer("Obstacle"));
    }

    void Start()
    {
        spawn = rb.position;
        PickPatrolPoint();
        AcquirePlayer();
    }

    void FixedUpdate()
    {
        if (dead) return;

        // Height is corrected even while shoved — a knocked-back bot that stopped
        // hovering would sit at whatever altitude the impulse left it at.
        Vector3 v = rb.linearVelocity;
        v.y = HoverVelocityY();

        if (Time.time < knockbackUntil)   // let the shove carry the body on XZ
        {
            rb.linearVelocity = v;
            return;
        }

        AcquirePlayer();
        UpdateState();

        Vector3 steer = state == State.Patrol ? Patrol() : Chase();
        v.x = steer.x;
        v.z = steer.z;
        rb.linearVelocity = v;
    }

    void Update()
    {
        if (dead) return;
        if (playerHealth != null) TryContactDamage();
        Animate();
    }

    // --- state ------------------------------------------------------------------------

    void UpdateState()
    {
        if (player == null) { state = State.Patrol; return; }

        float d = FlatDistance(player.position);
        if (state == State.Patrol)
        {
            if (d <= sightRadius && HasLineOfSight()) state = State.Chase;
        }
        else if ((d > giveUpRadius || !HasLineOfSight()) && Time.time >= provokedUntil)
        {
            state = State.Patrol;
            PickPatrolPoint();
        }
    }

    // The ray is the orb's own path — same origin, same flat direction, same length —
    // so the bot can never claim a shot it would only splash against a crate.
    bool HasLineOfSight()
    {
        if (player == null) return false;
        Vector3 origin = firePoint.position;
        Vector3 to = AimDirection(origin, out float distance);
        return distance <= 0.01f ||
               !Physics.Raycast(origin, to, distance, sightBlockers, QueryTriggerInteraction.Ignore);
    }

    // --- movement ---------------------------------------------------------------------

    Vector3 Patrol()
    {
        Vector3 to = targetPoint - rb.position;
        to.y = 0f;

        repathTimer -= Time.fixedDeltaTime;
        if (repathTimer <= 0f || to.sqrMagnitude <= 0.04f)
        {
            PickPatrolPoint();
            to = targetPoint - rb.position;
            to.y = 0f;
        }

        if (to.sqrMagnitude <= 0.04f) return Vector3.zero;
        Vector3 dir = to.normalized;
        facing = dir;
        return dir * patrolSpeed;
    }

    void PickPatrolPoint()
    {
        Vector2 r = Random.insideUnitCircle * wanderRadius;
        targetPoint = spawn + new Vector3(r.x, 0f, r.y);
        repathTimer = Random.Range(minRepathTime, maxRepathTime);
    }

    Vector3 Chase()
    {
        if (player == null) return Vector3.zero;

        Vector3 dir = AimDirection(rb.position, out float d);
        if (d > 0.01f) facing = dir;

        // Hold a preferred range: close in if far, back off if too close, hover if inside
        // the band. The 0.5 m dead zone is the 2D one — without it the bot judders.
        Vector3 move = Vector3.zero;
        if (d > preferredRange + 0.5f) move = dir;
        else if (d < preferredRange - 0.5f) move = -dir;

        if (Time.time >= nextFireTime && d > 0.1f && HasLineOfSight()) Fire();
        return move * chaseSpeed;
    }

    // A downward probe, not a fixed altitude: the bot holds its height above whatever is
    // actually beneath it, so it clears crates and catwalks instead of flying into them.
    float HoverVelocityY()
    {
        Vector3 origin = rb.position + Vector3.up * 0.2f;
        if (!Physics.Raycast(origin, Vector3.down, out var hit, hoverHeight + 20f,
                             groundMask, QueryTriggerInteraction.Ignore))
            return 0f;   // nothing below (out over a pit) → just hold the current height

        float error = (hit.point.y + hoverHeight) - rb.position.y;
        return Mathf.Clamp(error * hoverStiffness, -maxClimbSpeed, maxClimbSpeed);
    }

    // --- combat -----------------------------------------------------------------------

    void Fire()
    {
        nextFireTime = Time.time + fireCooldown;
        if (smogOrbPrefab == null) return;

        Vector3 dir = AimDirection(firePoint.position, out _);
        var orb = Instantiate(smogOrbPrefab, firePoint.position, Quaternion.LookRotation(dir, Vector3.up));
        if (orb.TryGetComponent<EnemyProjectile>(out var proj)) proj.Launch(dir);
        if (shootSfx != null) Sfx.Play(shootSfx, transform.position);
    }

    void TryContactDamage()
    {
        if (FlatDistance(player.position) > contactRange) return;
        playerHealth.TakeDamage(contactDamage, transform.position, contactKnockback);
    }

    public void TakeDamage(int amount)
    {
        if (dead) return;
        health -= amount;
        if (health <= 0) { Die(); return; }
        if (flash != null) flash.Flash();

        // A hit alerts it (2D did the same) — provokedUntil is what stops the alert
        // being undone by the very next UpdateState when the shot came from range.
        provokedUntil = Time.time + provokeDuration;
        state = State.Chase;
    }

    public void ApplyKnockback(Vector3 impulse, float duration)
    {
        if (dead) return;
        impulse.y = 0f;   // shoves stay on the ground plane; hover owns Y
        rb.linearVelocity = new Vector3(impulse.x, rb.linearVelocity.y, impulse.z);
        knockbackUntil = Time.time + duration;
    }

    void Die()
    {
        dead = true;
        SceneProgress.MarkConsumed(sceneName, spawnId);
        Codex.RecordKill(BestiaryCatalog.PollutionFlyBot);
        if (Random.value < 0.5f) Inventory.TryAdd(Random.value < 0.5f ? "bottle" : "scrap", 1);
        if (GameManager.Instance != null) GameManager.Instance.AddTrash(trashDropped);
        if (deathSfx != null) Sfx.Play(deathSfx, transform.position);

        // C4: the poof goes off at the body, up where the bot actually is — this is the one
        // enemy whose death is not at ground level, and a burst at its feet would look wrong.
        Vfx.Poof(transform.position, Vfx.ColorOf(gameObject, DeathTint), 1.1f);
        GameFeel.Shake(0.12f, 0.06f);
        GameFeel.HitStop(GameFeel.StopSmall);
        Destroy(gameObject);
    }

    // --- presentation -----------------------------------------------------------------

    void Animate()
    {
        if (visual == null) return;

        bobPhase += Time.deltaTime * bobSpeed * Mathf.PI * 2f;
        visual.localPosition = baseVisualPos + new Vector3(0f, Mathf.Sin(bobPhase) * bobAmplitude, 0f);

        if (facing.sqrMagnitude > 0.0001f)
        {
            Quaternion target = Quaternion.LookRotation(facing, Vector3.up);
            visual.rotation = Quaternion.RotateTowards(visual.rotation, target, turnSpeed * Time.deltaTime);
        }
    }

    // --- helpers ----------------------------------------------------------------------

    void AcquirePlayer()
    {
        if (playerHealth != null) return;
        if (Time.time < nextPlayerScan) return;
        nextPlayerScan = Time.time + 0.5f;

        var go = GameObject.FindGameObjectWithTag("Player");
        if (go == null) return;
        player = go.transform;
        playerHealth = go.GetComponent<PlayerHealth>();
    }

    /// <summary>Flat unit vector from <paramref name="from"/> to the player, plus that distance.</summary>
    Vector3 AimDirection(Vector3 from, out float distance)
    {
        Vector3 to = player.position - from;
        to.y = 0f;
        distance = to.magnitude;
        return distance > 0.0001f ? to / distance : facing;
    }

    float FlatDistance(Vector3 world)
    {
        Vector3 d = world - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, giveUpRadius);
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.5f);
        Gizmos.DrawWireSphere(Application.isPlaying ? spawn : transform.position, wanderRadius);
    }
}
