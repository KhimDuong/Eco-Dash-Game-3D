using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Cổng dịch chuyển (teleport gate) to the next level. Dormant until every Clean
/// Energy Core is collected (<see cref="GameManager.OnAllCoresCollected"/>), then it
/// powers up — green glow, faster spin, pulse — and the reclaimed pad beneath it
/// blooms. Walking into the live gate advances the level via
/// <see cref="GameManager.EnterGate"/>.
///
/// <para>Set <see cref="overrideTargetScene"/> to re-point a level's exit at the
/// central hub instead of the next build-index level (Stage 1 returns to the hub
/// rather than going straight to the Factory).</para>
///
/// <para>3D port: the portal now spins around <b>Y</b> (it was Z on the sprite) and
/// its colour goes through <see cref="MaterialTint"/>. Bob and pulse are unchanged.</para>
/// </summary>
[RequireComponent(typeof(Collider))]
public class TeleportGate : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("The glowing portal mesh that bobs and spins inside the gate.")]
    [SerializeField] Transform portal;
    [Tooltip("Reclaimed-ground pad under the gate; revealed when the gate powers up.")]
    [SerializeField] ReclamationPatch pad;

    [Header("Look")]
    [SerializeField] Color dormantColor = new Color(0.35f, 0.36f, 0.40f, 1f);
    [SerializeField] Color activeColor = new Color(0.45f, 1f, 0.65f, 1f);
    [SerializeField] float dormantScale = 1.1f;
    [SerializeField] float activeScale = 2.0f;

    [Header("Motion")]
    [SerializeField] float bobHeight = 0.15f;
    [SerializeField] float bobSpeed = 2f;
    [SerializeField] float idleSpinSpeed = 35f;
    [SerializeField] float activeSpinSpeed = 150f;
    [SerializeField] float pulseAmount = 0.12f;
    [SerializeField] float pulseSpeed = 4f;

    [Header("Audio (optional)")]
    [SerializeField] AudioClip activateSfx;

    [Header("Destination")]
    [Tooltip("If set, walking into the live gate loads this scene directly (e.g. the hub) " +
             "instead of GameManager.EnterGate's next-level navigation. Leave empty for L2.")]
    [SerializeField] string overrideTargetScene = "";

    Renderer[] portalRenderers;
    Vector3 portalBase;
    bool active;

    /// <summary>True once every core is in and the gate is live.</summary>
    public bool IsActive => active;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (portal != null)
        {
            portalRenderers = portal.GetComponentsInChildren<Renderer>(true);
            portalBase = portal.localPosition;
            ApplyColor(dormantColor);
            portal.localScale = Vector3.one * dormantScale;
        }
    }

    void Start()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnAllCoresCollected += Activate;
        if (GameManager.Instance.AllCoresCollected) Activate(); // safety on re-entry
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnAllCoresCollected -= Activate;
    }

    void Update()
    {
        if (portal == null) return;

        portal.Rotate(0f, (active ? activeSpinSpeed : idleSpinSpeed) * Time.deltaTime, 0f);

        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        portal.localPosition = portalBase + Vector3.up * bob;

        float pulse = active ? 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount : 1f;
        portal.localScale = Vector3.one * (active ? activeScale : dormantScale) * pulse;
    }

    public void Activate()
    {
        if (active) return;
        active = true;
        ApplyColor(activeColor);
        if (pad != null) pad.Reveal();
        if (activateSfx != null) Sfx.Play(activateSfx, transform.position);
        Debug.Log("[Eco-Dash] Teleport gate online — walk in to advance.");
    }

    void ApplyColor(Color c)
    {
        if (portalRenderers == null) return;
        foreach (var r in portalRenderers) MaterialTint.Apply(r, c);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!active || !other.CompareTag("Player")) return;
        if (!string.IsNullOrEmpty(overrideTargetScene))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(overrideTargetScene);
            return;
        }
        if (GameManager.Instance != null) GameManager.Instance.EnterGate();
    }
}
