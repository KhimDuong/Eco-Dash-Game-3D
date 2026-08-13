using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Slime Chúa (Slime King) — Level 1's mini-boss (M9, K8). A beefy slime that idles in
/// its grove until Greenie comes close, then chases and deals contact damage. At half
/// health it splits off a few ordinary <see cref="PlasticSlime"/>s. On death it records a
/// bestiary entry and <b>drops a Mảnh Cổng (Portal Shard)</b> — one of the shards the
/// hub's Stage-2 portal needs to power up.
///
/// <para><b>Tier 2 (redesigned for 3D)</b>, and redesigned the same way
/// <see cref="PlasticSlime"/> was, because it is the same animal one size up. Every stat,
/// drop and damage number is the 2D one; what changed is the plumbing:</para>
/// <list type="bullet">
/// <item><b>It walks the NavMesh.</b> The 2D King drove its <c>Rigidbody2D</c> straight at
/// the player, which only works on an open field. Here a <see cref="NavMeshAgent"/> takes
/// it around the huts, fences and rubble of the farm.</item>
/// <item><b>Contact damage is a distance test</b>, not <c>OnCollisionStay</c> — an agent
/// moves its transform kinematically and the player is a CharacterController, so that pair
/// never generates collision callbacks at all. The player's i-frames still gate the rate,
/// exactly as in 2D.</item>
/// <item><b>The split is a ring, not a scatter.</b> Random offsets could drop a minion
/// inside a fence or off the mesh; each spawn point is stepped evenly around the King and
/// then pulled onto the NavMesh.</item>
/// </list>
///
/// <para>The boss events are the 2D ones, now behind <see cref="IBoss"/> so
/// <see cref="BossHealthBar"/> can drive itself off either boss.</para>
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class SlimeKing : MonoBehaviour, IDamageable, IKnockbackable, IBoss
{
    [Header("Identity")]
    [SerializeField] string displayName = "Slime Chúa";

    [Header("Stats")]
    [SerializeField] int maxHealth = 20;
    [SerializeField] int contactDamage = 2;
    [Tooltip("Impulse pushed onto the player when the King lands a contact hit.")]
    [SerializeField] float contactKnockback = 9f;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 2.2f;
    [Tooltip("Greenie must be within this range for the boss to wake and chase.")]
    [SerializeField] float aggroRange = 7f;
    [SerializeField] float chaseRepathInterval = 0.25f;
    [Tooltip("How far to search for the nearest walkable point when the King is placed off-mesh.")]
    [SerializeField] float navMeshSnapRadius = 4f;

    [Header("Contact")]
    [Tooltip("Centre-to-centre distance on the ground plane that counts as touching. " +
             "Larger than a slime's because the King is.")]
    [SerializeField] float contactRange = 1.5f;

    [Header("Split (at half health)")]
    [Tooltip("Smaller slime spawned when the King drops to half HP (optional).")]
    [SerializeField] PlasticSlime minionPrefab;
    [SerializeField] int minionCount = 3;
    [Tooltip("Radius of the ring the minions are placed on.")]
    [SerializeField] float minionRing = 1.8f;

    [Header("Drops")]
    [SerializeField] string dropItemId = "portal_shard";
    [SerializeField] int dropAmount = 1;

    [Header("Feedback (optional)")]
    [SerializeField] HitFlash flash;
    [SerializeField] AudioClip deathSfx;

    /// <inheritdoc/>
    public event Action OnEngaged;
    /// <inheritdoc/>
    public event Action<int, int> OnHealthChanged;
    /// <inheritdoc/>
    public event Action OnDefeated;

    public string DisplayName => displayName;
    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsEngaged => engaged && !dead;

    NavMeshAgent agent;
    Transform player;
    PlayerHealth playerHealth;
    float knockbackUntil, nextChaseRepath, nextPlayerScan, nextNavMeshScan;
    bool engaged, split, dead, warnedOffMesh;

    // Same reason as PlasticSlime: SceneProgress ids an object by name + position, so
    // anything that walks must bank the id it had where it *spawned*, not where it died.
    string sceneName, spawnId;

    void Awake()
    {
        // Already defeated on a previous visit? Stay down (M9 save persistence).
        sceneName = SceneManager.GetActiveScene().name;
        spawnId = SceneProgress.IdFor(gameObject);
        if (SceneProgress.IsConsumed(sceneName, spawnId)) { Destroy(gameObject); return; }

        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.updateRotation = true;
        agent.updateUpAxis = true;
        CurrentHealth = maxHealth;
        if (flash == null) flash = GetComponent<HitFlash>();
    }

    void Start()
    {
        EnsureOnNavMesh();
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    void Update()
    {
        if (dead) return;

        // Contact damage runs even while knocked back — in 2D OnCollisionStay2D fired
        // regardless of the movement guard, and i-frames gate the rate anyway.
        if (AcquirePlayer()) TryContactDamage();

        if (!agent.isOnNavMesh) { EnsureOnNavMesh(); return; }
        if (Time.time < knockbackUntil) return;
        if (player == null) return;

        if (!engaged)
        {
            if (FlatDistance(player.position) > aggroRange) return;
            Engage();
        }

        if (Time.time < nextChaseRepath) return;
        nextChaseRepath = Time.time + chaseRepathInterval;
        agent.SetDestination(player.position);
    }

    void Engage()
    {
        if (engaged) return;
        engaged = true;
        agent.speed = moveSpeed;
        nextChaseRepath = 0f;
        BossHealthBar.Bind(this);      // no scene wiring — the bar builds itself
        OnEngaged?.Invoke();
        Debug.Log("[Eco-Dash] Slime Chúa awakened.");
    }

    // --- combat -------------------------------------------------------------------------

    void TryContactDamage()
    {
        if (FlatDistance(player.position) > contactRange) return;
        playerHealth.TakeDamage(contactDamage, transform.position, contactKnockback);
    }

    public void TakeDamage(int amount)
    {
        if (dead) return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        if (flash != null) flash.Flash();

        // Being shot wakes it, exactly as a Seed provokes an ordinary slime — otherwise a
        // player who opens from outside aggroRange fights a statue.
        if (!engaged) Engage();

        if (!split && CurrentHealth <= maxHealth / 2) Split();
        if (CurrentHealth == 0) Die();
    }

    public void ApplyKnockback(Vector3 impulse, float duration)
    {
        if (dead || !agent.isOnNavMesh) return;
        impulse.y = 0f;                  // knockback never launches anything upward
        agent.ResetPath();
        agent.velocity = impulse;        // the agent's own deceleration bleeds it off
        knockbackUntil = Time.time + duration;
        nextChaseRepath = 0f;
    }

    /// <summary>
    /// Half health: shed a ring of ordinary slimes. Each point is stepped evenly around the
    /// King (a random scatter can put two minions inside the same centimetre, and
    /// <see cref="SceneProgress"/> keys on rounded position) and then sampled onto the
    /// NavMesh, so a minion never lands inside a fence or off the mesh where it would freeze.
    /// </summary>
    void Split()
    {
        split = true;
        if (minionPrefab == null || minionCount <= 0) return;

        for (int i = 0; i < minionCount; i++)
        {
            float ang = i * Mathf.PI * 2f / minionCount;
            Vector3 at = transform.position + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * minionRing;
            if (NavMesh.SamplePosition(at, out var hit, minionRing, NavMesh.AllAreas)) at = hit.position;
            Instantiate(minionPrefab, at, Quaternion.identity);
        }
        Debug.Log($"[Eco-Dash] Slime Chúa split into {minionCount} slimes.");
    }

    void Die()
    {
        if (dead) return;
        dead = true;
        if (agent.isOnNavMesh) agent.ResetPath();
        agent.enabled = false;

        SceneProgress.MarkConsumed(sceneName, spawnId);
        Codex.RecordKill(BestiaryCatalog.SlimeKing);
        if (!string.IsNullOrEmpty(dropItemId)) Inventory.TryAdd(dropItemId, dropAmount);
        if (deathSfx != null) AudioSource.PlayClipAtPoint(deathSfx, transform.position);
        OnDefeated?.Invoke();
        Debug.Log("[Eco-Dash] Slime Chúa defeated — Mảnh Cổng dropped.");
        Destroy(gameObject);
    }

    // --- helpers ------------------------------------------------------------------------

    // Dropped inside a prop (or spawned before the NavMesh loads) the King has no agent at
    // all; snap it to the nearest walkable point instead of leaving a statue in the grove.
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
        Gizmos.color = new Color(1f, 0.35f, 0.75f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, contactRange);
    }
}
