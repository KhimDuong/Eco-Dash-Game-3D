using System.Collections;
using UnityEngine;

/// <summary>
/// Thẻ từ (security keycard) — a Level 2 objective pickup that reuses the Level 1
/// core plumbing: interacting with E calls <see cref="GameManager.CollectCore"/>,
/// and collecting every keycard raises <see cref="GameManager.OnAllCoresCollected"/>,
/// which unlocks the <see cref="BossDoor"/>. Themed for the factory (no reclamation
/// patch); the card pops up and chimes when grabbed. Same IInteractable contract as
/// <see cref="Chest"/>, driven by <see cref="PlayerInteractor"/>.
///
/// <para>3D port (Tier 1): colliders and renderers swap dimension. The grab pop was
/// already a rise along <b>Y</b>, so it survives untouched — but the 2D fade-out was
/// an alpha ramp on a sprite, and the greybox meshes are opaque, so the card shrinks
/// as it rises instead. Alpha is still pushed through <see cref="MaterialTint"/> so
/// the fade returns for free once a transparent material is used in the P3 art pass.</para>
/// </summary>
[RequireComponent(typeof(Collider))]
public class Keycard : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    [Tooltip("\"Nhấn E\" label shown only while Greenie is the nearest interactable.")]
    [SerializeField] GameObject prompt;

    [Header("Audio")]
    [SerializeField] AudioClip grabSfx;

    [Header("Feel")]
    [Tooltip("Mesh that rises and fades on pickup. Defaults to the first child renderer.")]
    [SerializeField] Renderer cardRenderer;
    [SerializeField] float popHeight = 0.6f;
    [SerializeField] float popDuration = 0.5f;

    bool taken;
    Vector3 baseScale;

    public bool CanInteract => !taken;
    public Vector3 InteractPosition => transform.position;
    public void SetHighlighted(bool highlighted)
    {
        if (prompt != null) prompt.SetActive(highlighted && !taken);
    }

    void Awake()
    {
        // Already taken on a previous visit? Stay gone (M9 save persistence).
        if (SceneProgress.IsConsumed(gameObject)) { Destroy(gameObject); return; }
        GetComponent<Collider>().isTrigger = true;
        if (prompt != null) prompt.SetActive(false);
        if (cardRenderer == null) cardRenderer = GetComponentInChildren<Renderer>();
        baseScale = transform.localScale;
    }

    public void Interact(GameObject interactor)
    {
        if (taken) return;
        taken = true;
        SceneProgress.Consume(gameObject);
        if (prompt != null) prompt.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.CollectCore();
        if (grabSfx != null) AudioSource.PlayClipAtPoint(grabSfx, transform.position);
        StartCoroutine(GrabRoutine());
    }

    IEnumerator GrabRoutine()
    {
        Vector3 from = transform.position;
        Vector3 to = from + Vector3.up * popHeight;
        Color tint = cardRenderer != null ? MaterialTint.Read(cardRenderer) : Color.white;
        float t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            float u = t / popDuration;
            transform.position = Vector3.Lerp(from, to, Mathf.Sqrt(u));   // ease-out rise
            transform.localScale = baseScale * (1f - u);
            if (cardRenderer != null) MaterialTint.Apply(cardRenderer, new Color(tint.r, tint.g, tint.b, 1f - u));
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
