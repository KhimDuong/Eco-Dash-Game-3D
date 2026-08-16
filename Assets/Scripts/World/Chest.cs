using System.Collections;
using UnityEngine;

/// <summary>
/// Rương gỗ cũ (old wooden chest) holding a Clean Energy Core. Greenie opens it
/// with E (via <see cref="PlayerInteractor"/>). Opening grants the core to the
/// <see cref="GameManager"/>, pops the core mesh out with a flourish, dims the
/// chest to read as emptied, and reveals its <see cref="ReclamationPatch"/> so the
/// dead ground around the chest turns lush green — the level's payoff beat.
///
/// <para>3D port: `Collider2D` → `Collider`, and the emptied tint goes through
/// <see cref="MaterialTint"/> instead of a SpriteRenderer's colour. The squash-punch
/// and the core's rise are unchanged — both were already on X/Y, and Y is still up.</para>
/// </summary>
[RequireComponent(typeof(Collider))]
public class Chest : MonoBehaviour, IInteractable
{
    /// <summary>What opening the chest grants.</summary>
    public enum Reward
    {
        EnergyCore, // advances the level objective (Level 1's 3 cores) via CollectCore
        Trash       // recycling currency only (a reward chest) via AddTrash — no objective change
    }

    [Header("Contents")]
    [Tooltip("EnergyCore advances the objective (Level 1); Trash just grants recycling currency " +
             "without touching the objective (e.g. a Level 2 reward chest).")]
    [SerializeField] Reward reward = Reward.EnergyCore;
    [Tooltip("Recycling currency granted when Reward = Trash.")]
    [SerializeField] int trashAmount = 10;
    [Tooltip("Energy-core mesh hidden inside; pops out on open. Cosmetic — the reward is granted directly.")]
    [SerializeField] Transform coreVisual;
    [Tooltip("Dead ground around the chest that turns green when this chest is opened.")]
    [SerializeField] ReclamationPatch patch;

    [Header("Prompt")]
    [Tooltip("\"Nhấn E\" label shown only while Greenie is the nearest interactable.")]
    [SerializeField] GameObject prompt;

    [Header("Audio (optional)")]
    [SerializeField] AudioClip openSfx;
    [SerializeField] AudioClip coreSfx;

    [Header("Feel")]
    [SerializeField] float corePopHeight = 1.1f;
    [SerializeField] float corePopDuration = 0.6f;
    [SerializeField] float openPunch = 0.18f;
    [SerializeField] Color emptiedTint = new Color(0.35f, 0.32f, 0.28f, 1f);

    bool opened;
    Vector3 baseScale;
    Renderer[] chestRenderers;

    // --- IInteractable ------------------------------------------------------
    public bool CanInteract => !opened;
    public Vector3 InteractPosition => transform.position;

    public void SetHighlighted(bool highlighted)
    {
        if (prompt != null) prompt.SetActive(highlighted && !opened);
    }

    public void Interact(GameObject interactor)
    {
        if (!opened) Open();
    }

    // ------------------------------------------------------------------------

    void Awake()
    {
        baseScale = transform.localScale;
        chestRenderers = GetComponentsInChildren<Renderer>(true);
        GetComponent<Collider>().isTrigger = true;
        if (coreVisual != null) coreVisual.gameObject.SetActive(false);
        if (prompt != null) prompt.SetActive(false);
    }

    void Start()
    {
        // Already looted on a previous visit? Come back emptied (M9 save persistence).
        if (!SceneProgress.IsConsumed(gameObject)) return;
        opened = true;
        Tint(emptiedTint);
        if (prompt != null) prompt.SetActive(false);
        if (patch != null) patch.ApplyFinalState();
    }

    void Open()
    {
        opened = true;
        SceneProgress.Consume(gameObject);
        if (prompt != null) prompt.SetActive(false);
        Tint(emptiedTint);

        if (GameManager.Instance != null)
        {
            if (reward == Reward.Trash) GameManager.Instance.AddTrash(trashAmount);
            else GameManager.Instance.CollectCore();
        }
        if (openSfx != null) Sfx.Play(openSfx, transform.position);
        if (patch != null) patch.Reveal();

        StartCoroutine(OpenRoutine());
    }

    void Tint(Color c)
    {
        if (chestRenderers == null) return;
        foreach (var r in chestRenderers)
        {
            if (coreVisual != null && r.transform.IsChildOf(coreVisual)) continue;
            MaterialTint.Apply(r, c);
        }
    }

    IEnumerator OpenRoutine()
    {
        // Squash-punch the chest so the lid pop reads with some weight.
        float t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            float k = 1f + openPunch * Mathf.Sin(t / 0.15f * Mathf.PI);
            transform.localScale = new Vector3(baseScale.x * k, baseScale.y * (2f - k), baseScale.z * k);
            yield return null;
        }
        transform.localScale = baseScale;

        // Float the core up and shrink it away, then ring the collect chime.
        if (coreVisual != null)
        {
            coreVisual.gameObject.SetActive(true);
            Vector3 from = transform.position;
            Vector3 to = from + Vector3.up * corePopHeight;
            Vector3 coreScale = coreVisual.localScale;
            float p = 0f;
            while (p < corePopDuration)
            {
                p += Time.deltaTime;
                float u = p / corePopDuration;
                coreVisual.position = Vector3.Lerp(from, to, Mathf.Sqrt(u)); // ease-out rise
                // Opaque meshes can't fade like the 2D sprite did — shrink out instead.
                coreVisual.localScale = coreScale * (1f - u);
                yield return null;
            }
            coreVisual.gameObject.SetActive(false);
            coreVisual.localScale = coreScale;
        }

        if (coreSfx != null) Sfx.Play(coreSfx, transform.position);
    }
}
