using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Máy Hủy Diệt Khói Độc — the "Mega-Smog" destruction machine, Level 2's boss and the last
/// fight in the game. A stationary, high-HP <see cref="IDamageable"/> at the centre of the
/// sealed arena. Once Greenie is through the blast door it <i>engages</i> (raising
/// <see cref="OnEngaged"/> so the boss bar appears) and alternates two attacks: a ring of
/// Smog Orbs, rotated a little each volley, and waves of <see cref="ToxicGasZone"/>s around
/// the player. Below an HP threshold it enrages — faster, denser, and permanently angry.
/// Death calls <see cref="GameManager.CompleteLevel"/>, which the Level 2 HUD routes to the
/// Ending_Story slides.
///
/// <para><b>Tier 2 (redesigned for 3D).</b> Every stat, timing and damage number is the 2D
/// one. Four things had to change:</para>
/// <list type="bullet">
/// <item><b>The spray is a ring on XZ.</b> The 2D fan swept a circle in screen space; here
/// it sweeps the ground plane, and the orbs leave at <see cref="fireHeight"/> — Greenie's
/// chest — because <see cref="EnemyProjectile"/> flies flat and a ring emitted from the top
/// of a 3 m machine would pass clean over his head.</item>
/// <item><b>Waking up is line-of-sight, not just distance.</b> The boss sits 4 m from the
/// blast door, well inside its 7 m activation radius, so a pure distance check had it
/// firing through a locked door at a player who cannot answer. The linecast runs against
/// Obstacle, which is the layer <see cref="BossDoor"/>'s blocker is on — so it wakes at the
/// moment the door opens and Greenie steps in, which is also when it reads best.</item>
/// <item><b>Gas lands where the player can stand.</b> Points around Greenie are pulled onto
/// the NavMesh; the arena is only 6 m deep, so a raw ±5 m scatter would drop half of each
/// wave inside a wall where it threatens nobody.</item>
/// <item><b>Contact damage is a distance test</b>, and the enrage tint goes through
/// <see cref="HitFlash.SetBaseTint"/> — see that method for why a boss cannot tint itself.</item>
/// </list>
///
/// <para><b>The hurtbox reaches the floor.</b> Greenie's Seeds fly flat at y ≈ 0.6; the
/// machine's own silhouette starts higher than that in places. The prefab therefore carries
/// a trigger box spanning the full body down to 0.1 m, on the <b>root</b>, because
/// projectiles resolve <see cref="IDamageable"/> off the collider they hit. Height is
/// presentation; hitting things is a question of XZ.</para>
/// </summary>
public class MegaSmogBoss : MonoBehaviour, IDamageable, IBoss
{
    [Header("Identity")]
    [SerializeField] string displayName = "Máy Hủy Diệt Khói Độc";

    [Header("Health")]
    [SerializeField] int maxHealth = 40;

    [Header("Engage")]
    [Tooltip("Boss wakes when the player comes within this distance with a clear line to them.")]
    [SerializeField] float activationRadius = 7f;
    [Tooltip("Layers that count as 'the door is still shut'. Defaults to Obstacle when left empty.")]
    [SerializeField] LayerMask sightBlockers;
    [Tooltip("Height the wake-up linecast runs at — chest height, so the floor never blocks it.")]
    [SerializeField] float sightHeight = 1f;

    [Header("Spray attack (ring of orbs)")]
    [SerializeField] GameObject smogOrbPrefab;
    [SerializeField] int sprayBullets = 8;
    [SerializeField] float spraySpeed = 5f;
    [Tooltip("Spray is rotated by this many degrees each volley (rotating fan).")]
    [SerializeField] float spinPerVolley = 11f;
    [Tooltip("How far out the orbs are born — must clear the machine's own body.")]
    [SerializeField] float sprayRadius = 1.9f;
    [Tooltip("Height the orbs leave at. Greenie's capsule is 1.15 m, so this must sit inside it.")]
    [SerializeField] float fireHeight = 0.9f;

    [Header("Gas attack")]
    [SerializeField] GameObject gasZonePrefab;
    [SerializeField] int gasZonesPerWave = 3;
    [SerializeField] float gasSpawnRadius = 5f;

    [Header("Cycle")]
    [SerializeField] float attackInterval = 2.2f;
    [Tooltip("Fraction of max HP at/under which the boss enrages.")]
    [SerializeField] float enrageThreshold = 0.35f;
    [Tooltip("Attack interval multiplier while enraged (<1 = faster).")]
    [SerializeField] float enrageRateMul = 0.6f;

    [Header("Contact")]
    [SerializeField] int contactDamage = 1;
    [SerializeField] float contactKnockback = 9f;
    [Tooltip("Centre-to-centre distance on the ground plane that counts as touching the machine.")]
    [SerializeField] float contactRange = 2.2f;

    [Header("Feedback")]
    [SerializeField] HitFlash flash;
    [Tooltip("Pulsing toxic core, so the machine reads as alive.")]
    [SerializeField] Transform core;
    [Tooltip("Permanent tint multiplied over the machine's colours once it enrages.")]
    [SerializeField] Color enrageTint = new Color(1f, 0.55f, 0.55f, 1f);
    [SerializeField] AudioClip deathSfx;
    [SerializeField] AudioClip attackSfx;

    /// <inheritdoc/>
    public event Action OnEngaged;
    /// <inheritdoc/>
    public event Action<int, int> OnHealthChanged;
    /// <inheritdoc/>
    public event Action OnDefeated;

    public string DisplayName => displayName;
    public int CurrentHealth => health;
    public int MaxHealth => maxHealth;
    public bool IsEngaged => engaged && !dead;

    /// <summary>True once the machine has passed its enrage threshold.</summary>
    public bool IsEnraged => enraged;

    int health;
    int enrageAt;               // whole HP, resolved once — see Awake
    bool dead, enraged, engaged;
    float sprayAngleOffset;
    Vector3 coreBaseScale = Vector3.one;
    Transform player;
    PlayerHealth playerHealth;
    float nextPlayerScan;

    string sceneName, spawnId;

    void Awake()
    {
        // Already defeated on a previous visit? Stay down (M9 save persistence).
        sceneName = SceneManager.GetActiveScene().name;
        spawnId = SceneProgress.IdFor(gameObject);
        if (SceneProgress.IsConsumed(sceneName, spawnId)) { Destroy(gameObject); return; }

        health = maxHealth;

        // Resolved to whole HP once, and rounded UP, because the 2D expression
        // `health <= maxHealth * enrageThreshold` does not mean what it reads as: 0.35f is
        // really 0.34999999, so 40 * 0.35f is 13.9999998 and the boss silently skipped its
        // enrage at 14 HP — it fired a whole point later, or not at all on other numbers.
        enrageAt = Mathf.CeilToInt(maxHealth * enrageThreshold);

        if (flash == null) flash = GetComponent<HitFlash>();
        if (core != null) coreBaseScale = core.localScale;
        if (sightBlockers == 0) sightBlockers = 1 << LayerMask.NameToLayer("Obstacle");
    }

    void Start() => OnHealthChanged?.Invoke(health, maxHealth);

    void Update()
    {
        if (dead) return;

        // Gentle core pulse so the machine reads as "alive" even before it wakes.
        if (core != null)
        {
            float pulse = 1f + 0.08f * Mathf.Sin(Time.time * (enraged ? 9f : 4f));
            core.localScale = coreBaseScale * pulse;
        }

        if (!AcquirePlayer()) return;
        if (engaged) TryContactDamage();
        else if (CanSeePlayer()) Engage();
    }

    // --- engaging -------------------------------------------------------------------------

    // Distance AND a clear line: the blast door's blocker sits on Obstacle and is disabled
    // when the door opens, so this is false for exactly as long as the arena is sealed.
    bool CanSeePlayer()
    {
        Vector3 from = transform.position + Vector3.up * sightHeight;
        Vector3 to = player.position + Vector3.up * sightHeight;
        Vector3 delta = to - from;
        delta.y = 0f;
        float d = delta.magnitude;
        if (d > activationRadius) return false;
        return d <= 0.01f ||
               !Physics.Raycast(from, delta / d, d, sightBlockers, QueryTriggerInteraction.Ignore);
    }

    void Engage()
    {
        engaged = true;
        BossHealthBar.Bind(this);       // no scene wiring — the bar builds itself
        OnEngaged?.Invoke();
        Debug.Log("[Eco-Dash] Mega-Smog online.");
        StartCoroutine(AttackLoop());
    }

    IEnumerator AttackLoop()
    {
        yield return new WaitForSeconds(0.8f);   // a beat before the first attack
        int toggle = 0;
        while (!dead)
        {
            if (toggle % 2 == 0) SprayVolley();
            else GasWave();
            toggle++;
            yield return new WaitForSeconds(attackInterval * (enraged ? enrageRateMul : 1f));
        }
    }

    // --- attacks --------------------------------------------------------------------------

    void SprayVolley()
    {
        if (smogOrbPrefab == null) return;
        if (attackSfx != null) AudioSource.PlayClipAtPoint(attackSfx, transform.position);

        sprayAngleOffset += spinPerVolley;
        int n = enraged ? sprayBullets + 4 : sprayBullets;
        Vector3 origin = transform.position + Vector3.up * fireHeight;

        for (int i = 0; i < n; i++)
        {
            float ang = (sprayAngleOffset + i * (360f / n)) * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            var orb = Instantiate(smogOrbPrefab, origin + dir * sprayRadius,
                                  Quaternion.LookRotation(dir, Vector3.up));
            if (orb.TryGetComponent<EnemyProjectile>(out var proj)) proj.Launch(dir, spraySpeed);
        }
    }

    void GasWave()
    {
        if (gasZonePrefab == null || player == null) return;
        int n = enraged ? gasZonesPerWave + 1 : gasZonesPerWave;
        for (int i = 0; i < n; i++)
        {
            Vector2 r = UnityEngine.Random.insideUnitCircle * gasSpawnRadius;
            Vector3 at = player.position + new Vector3(r.x, 0f, r.y);
            // Onto the floor the player can actually be standing on — the arena is 6 m deep.
            if (NavMesh.SamplePosition(at, out var hit, gasSpawnRadius, NavMesh.AllAreas))
                at = hit.position;
            at.y = 0f;
            Instantiate(gasZonePrefab, at, Quaternion.identity);
        }
    }

    void TryContactDamage()
    {
        if (playerHealth == null) return;
        Vector3 d = player.position - transform.position;
        d.y = 0f;
        if (d.magnitude > contactRange) return;
        playerHealth.TakeDamage(contactDamage, transform.position, contactKnockback);
    }

    // --- damage ---------------------------------------------------------------------------

    public void TakeDamage(int amount)
    {
        if (dead) return;
        health = Mathf.Max(0, health - amount);
        OnHealthChanged?.Invoke(health, maxHealth);
        if (flash != null) flash.Flash();

        // Shooting the machine through the open door wakes it even if the player never
        // walked into the activation radius — the 2D boss only woke on approach, which
        // let a patient player chip it down from the doorway for free.
        if (!engaged) Engage();

        if (!enraged && health <= enrageAt) Enrage();
        if (health == 0) Die();
    }

    void Enrage()
    {
        enraged = true;
        if (flash != null) flash.SetBaseTint(enrageTint);
        Debug.Log("[Eco-Dash] Mega-Smog enraged.");
    }

    void Die()
    {
        if (dead) return;
        dead = true;
        SceneProgress.MarkConsumed(sceneName, spawnId);
        Codex.RecordKill(BestiaryCatalog.MegaSmog);
        if (deathSfx != null) AudioSource.PlayClipAtPoint(deathSfx, transform.position);
        OnDefeated?.Invoke();
        Debug.Log("[Eco-Dash] Mega-Smog destroyed — the valley can breathe.");
        StartCoroutine(DeathRoutine());
    }

    /// <summary>
    /// Collapse, then win. The 2D machine faded its sprite's alpha out; an opaque URP mesh
    /// has no alpha to fade, so it crumples instead — shrinking, spinning and sinking into
    /// the floor, which reads at least as well from the fixed ¾ camera.
    /// </summary>
    IEnumerator DeathRoutine()
    {
        const float dur = 1.1f;
        Vector3 scale0 = transform.localScale;
        Vector3 pos0 = transform.position;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            transform.localScale = Vector3.Lerp(scale0, scale0 * 0.2f, u);
            transform.position = pos0 + Vector3.down * (1.6f * u);
            transform.Rotate(0f, 540f * Time.deltaTime, 0f, Space.World);
            yield return null;
        }
        if (GameManager.Instance != null) GameManager.Instance.CompleteLevel();
        Destroy(gameObject);
    }

    // --- helpers --------------------------------------------------------------------------

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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, contactRange);
        Gizmos.color = new Color(0.6f, 0.9f, 0.3f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, gasSpawnRadius);
    }
}
