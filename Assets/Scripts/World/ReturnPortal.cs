using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Cổng Về Trạm (return portal) placed in each stage (M9, K7) — the reverse travel
/// the one-way <see cref="TeleportGate"/> lacks. Always available; walk into it (or
/// press E) to go back to the central hub, enabling the full hub ↔ stage ↔ stage
/// loop. State (inventory / quests / cleanliness) persists across the hop via the
/// static stores.
///
/// <para>3D port (Tier 1): <c>Collider2D</c> → <see cref="Collider"/>, nothing else.</para>
/// </summary>
[RequireComponent(typeof(Collider))]
public class ReturnPortal : MonoBehaviour, IInteractable
{
    [SerializeField] string hubScene = "Shop_RecyclingStation";
    [Tooltip("If true, returns on touch; otherwise needs the E key.")]
    [SerializeField] bool walkOver = true;
    [SerializeField] GameObject prompt;
    [SerializeField] AudioClip travelSfx;

    public bool CanInteract => !walkOver && !DialogueRunner.IsActive;
    public Vector3 InteractPosition => transform.position;
    public void SetHighlighted(bool highlighted)
    {
        if (prompt != null) prompt.SetActive(highlighted && CanInteract);
    }

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (prompt != null) prompt.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!walkOver) return;
        if (other.CompareTag("Player")) Travel();
    }

    public void Interact(GameObject interactor)
    {
        if (!walkOver) Travel();
    }

    void Travel()
    {
        if (travelSfx != null) Sfx.Play(travelSfx, transform.position);
        Time.timeScale = 1f;
        SceneManager.LoadScene(hubScene);
    }
}
