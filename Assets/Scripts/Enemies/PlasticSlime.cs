using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Level 1 enemy — Quái Rác Nhựa (Plastic Slime). Wanders around its spawn point,
/// charges the player once he comes close, and deals contact damage. Dies to Seed
/// projectiles and drops trash + recycling materials on death.
///
/// <para><b>Tier 2 (redesigned for 3D).</b> The 2D slime steered a
/// <c>Rigidbody2D</c> straight at a random point near its spawn, which works only
/// because the 2D farm is an open field. In 3D it walks a baked NavMesh through a
/// <see cref="NavMeshAgent"/>, so it rounds the huts, fences and rubble instead of
/// grinding into them. Every stat, drop and damage number is the 2D one; the two
/// genuinely new things are the <b>aggro radius</b> that C1 asks for (the bestiary
/// already described the slime as charging at Greenie — the 2D code never did) and
/// the flash moving from a sprite tint to <see cref="HitFlash"/>.</para>
///
/// <para>Contact damage is a distance test rather than <c>OnCollisionStay</c>: a
/// NavMeshAgent moves its transform kinematically and the player is a
/// CharacterController, so the pair never generates collision callbacks. The
/// player's invulnerability frames still gate the rate, exactly as in 2D.</para>
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class PlasticSlime : MonoBehaviour, IDamageable, IKnockbackable
{
    [Header("Stats")]
    [SerializeField] int maxHealth = 2;
    [SerializeField] int contactDamage = 1;
    [SerializeField] int trashDropped = 1;
    [Tooltip("Impulse pushed onto the player when the slime lands a contact hit.")]
    [SerializeField] float contactKnockback = 7f;

    [Header("Feedback (optional)")]
    [SerializeField] HitFlash flash;
    [SerializeField] AudioClip deathSfx;

    // Only used if the death poof can't read a colour off the art — the art pass owns the
    // real one, so this never needs updating when the slime is recoloured.
    static readonly Color DeathTint = new Color(0.42f, 0.70f, 0.45f);

    [Header("Patrol")]
    [SerializeField] float moveSpeed = 1.5f;
    [Tooltip("How far from its spawn the slime will wander.")]
    [SerializeField] float wanderRadius = 3f;
    [SerializeField] float minRepathTime = 1.5f;
    [SerializeField] float maxRepathTime = 3.5f;
    [Tooltip("How close counts as 'reached' the target point.")]
    [SerializeField] float arriveDistance = 0.2f;

    [Header("Aggro (new in 3D)")]
    [Tooltip("Greenie inside this radius makes the slime charge.")]
    [SerializeField] float aggroRadius = 6f;
    [Tooltip("Chase is dropped once he gets this far away — hysteresis, so it can't flicker.")]
    [SerializeField] float loseAggroRadius = 9f;
    [Tooltip("Seconds a Seed hit keeps the slime chasing even from outside loseAggroRadius. " +
             "Without it, sniping from range aggros and de-aggros on the very next frame.")]
    [SerializeField] float provokeDuration = 4f;
    [Tooltip("Speed while charging. Deliberately below the player's 5 m/s: a slime is a " +
             "threat you have to deal with, not one you can never escape.")]
    [SerializeField] float chaseSpeed = 2.5f;
    [SerializeField] float chaseRepathInterval = 0.25f;

    [Header("Contact")]
    [Tooltip("Centre-to-centre distance on the ground plane that counts as touching.")]
    [SerializeField] float contactRange = 0.9f;

    [Header("NavMesh")]
    [Tooltip("How far to search for the nearest walkable point when the slime is placed off-mesh.")]
    [SerializeField] float navMeshSnapRadius = 3f;

    NavMeshAgent agent;
    Vector3 spawnPoint;
    Vector3 targetPoint;
    float repathTimer;
    float nextChaseRepath;
    float knockbackUntil;        // steering is suppressed until this time
    float provokedUntil;         // a recent hit keeps the chase alive at any range
    bool recovering;             // knockback just ended → repath immediately
    bool aggro;
    int health;
    bool dead;

    Transform player;
    PlayerHealth playerHealth;
    float nextPlayerScan;
    float nextNavMeshScan;
    bool warnedOffMesh;

    // SceneProgress ids an object by name + position, which is fine for a chest but not
    // for something that walks: a slime killed 10 m from its spawn would bank an id no
    // freshly-placed slime ever matches, and would be back on the next load. Pin the id
    // to where it *starts*, which is the one thing the scene and the save agree on.
    string sceneName;
    string spawnId;

    /// <summary>True while the slime is charging the player rather than wandering.</summary>
    public bool IsAggro => aggro;
    public int Health => health;

    void Awake()
    {
        // Already killed on a previous visit? Stay dead (M9 save persistence).
        sceneName = SceneManager.GetActiveScene().name;
        spawnId = SceneProgress.IdFor(gameObject);
        if (SceneProgress.IsConsumed(sceneName, spawnId)) { Destroy(gameObject); return; }

        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = true;
        agent.updateUpAxis = true;
        agent.speed = moveSpeed;
        health = maxHealth;
        if (flash == null) flash = GetComponent<HitFlash>();
    }

    void Start()
    {
        EnsureOnNavMesh();
        spawnPoint = transform.position;
        PickNewTarget();
    }

    void Update()
    {
        if (dead) return;

        // Contact damage runs even while knocked back — in 2D OnCollisionStay2D
        // fired regardless of the patrol guard, and i-frames gate the rate anyway.
        if (AcquirePlayer()) TryContactDamage();

        if (!agent.isOnNavMesh) { EnsureOnNavMesh(); return; }
        if (Time.time < knockbackUntil) { recovering = true; return; }

        if (recovering)
        {
            recovering = false;
            nextChaseRepath = 0f;
            if (!aggro) PickNewTarget();
        }

        if (playerHealth != null) UpdateAggro();
        if (aggro) Chase();
        else Patrol();
    }

    // --- movement -----------------------------------------------------------------

    void UpdateAggro()
    {
        float d = FlatDistance(player.position);
        if (!aggro && d <= aggroRadius) Engage();
        else if (aggro && d > loseAggroRadius && Time.time >= provokedUntil)
        {
            aggro = false;
            agent.speed = moveSpeed;
            PickNewTarget();
        }
    }

    void Engage()
    {
        aggro = true;
        agent.speed = chaseSpeed;
        nextChaseRepath = 0f;
    }

    void Chase()
    {
        if (Time.time < nextChaseRepath) return;
        nextChaseRepath = Time.time + chaseRepathInterval;
        agent.SetDestination(player.position);
    }

    void Patrol()
    {
        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f || FlatDistance(targetPoint) <= arriveDistance) PickNewTarget();
    }

    void PickNewTarget()
    {
        // Same random-destination patrol as 2D: a point inside wanderRadius of the
        // spawn, then pulled onto the NavMesh so the agent can actually reach it.
        Vector2 r = Random.insideUnitCircle * wanderRadius;
        Vector3 candidate = spawnPoint + new Vector3(r.x, 0f, r.y);
        targetPoint = NavMesh.SamplePosition(candidate, out var hit, wanderRadius, NavMesh.AllAreas)
            ? hit.position
            : spawnPoint;

        if (agent.isOnNavMesh) agent.SetDestination(targetPoint);
        repathTimer = Random.Range(minRepathTime, maxRepathTime);
    }

    // A slime dropped inside a prop (or before the NavMesh loads) has no agent at all;
    // snap it to the nearest walkable point instead of leaving a statue in the field.
    void EnsureOnNavMesh()
    {
        if (agent.isOnNavMesh || Time.time < nextNavMeshScan) return;
        nextNavMeshScan = Time.time + 1f;

        if (NavMesh.SamplePosition(transform.position, out var hit, navMeshSnapRadius, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            return;
        }
        if (!warnedOffMesh)
        {
            warnedOffMesh = true;
            Debug.LogWarning($"[Eco-Dash] {name} is {navMeshSnapRadius} m from any NavMesh — " +
                             "is the scene's NavMesh baked?", this);
        }
    }

    // --- combat -------------------------------------------------------------------

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

        // Being shot is provocation enough — a sniped slime comes for you even from
        // outside its aggro radius, and provokedUntil holds the chase long enough for
        // the hysteresis in UpdateAggro not to drop it on the very next frame.
        provokedUntil = Time.time + provokeDuration;
        if (!aggro && agent.isOnNavMesh) Engage();
    }

    public void ApplyKnockback(Vector3 impulse, float duration)
    {
        if (dead || !agent.isOnNavMesh) return;
        impulse.y = 0f;                       // knockback never launches anything upward
        agent.ResetPath();
        agent.velocity = impulse;             // the agent's own deceleration bleeds it off
        knockbackUntil = Time.time + duration;
    }

    void Die()
    {
        dead = true;
        SceneProgress.MarkConsumed(sceneName, spawnId);
        Codex.RecordKill(BestiaryCatalog.PlasticSlime);
        if (Random.value < 0.5f) Inventory.TryAdd(Random.value < 0.5f ? "bottle" : "scrap", 1);
        if (GameManager.Instance != null) GameManager.Instance.AddTrash(trashDropped);
        if (deathSfx != null) Sfx.Play(deathSfx, transform.position);

        // C4: burst in its own colour, read live off the art so a recolour in ArtPass
        // carries through without touching the prefab.
        Vfx.Poof(transform.position + Vector3.up * 0.35f, Vfx.ColorOf(gameObject, DeathTint));
        GameFeel.Shake(0.1f, 0.05f);
        GameFeel.HitStop(GameFeel.StopSmall);
        Destroy(gameObject);
    }

    // --- helpers ------------------------------------------------------------------

    bool AcquirePlayer()
    {
        if (playerHealth != null) return true;
        if (Time.time < nextPlayerScan) return false;
        nextPlayerScan = Time.time + 0.5f;

        var go = GameObject.FindGameObjectWithTag("Player");
        if (go == null) return false;
        player = go.transform;
        playerHealth = go.GetComponent<PlayerHealth>();
        return playerHealth != null;
    }

    float FlatDistance(Vector3 world)
    {
        Vector3 d = world - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.55f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, aggroRadius);
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.35f);
        Gizmos.DrawWireSphere(Application.isPlaying ? spawnPoint : transform.position, wanderRadius);
    }
}
