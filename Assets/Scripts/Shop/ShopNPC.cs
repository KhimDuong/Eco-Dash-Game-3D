using UnityEngine;

/// <summary>
/// Ông Bear (Mr. Bear) — the recycling-station shopkeeper. An
/// <see cref="IInteractable"/> reusing <see cref="PlayerInteractor"/>: Greenie
/// walks up and presses E to open the <see cref="ShopController"/> window. The
/// "Nhấn E" prompt hides while the shop is open.
///
/// <para>3D port (Tier 1): <c>Collider2D</c> → <see cref="Collider"/>. Note that
/// <see cref="PlayerInteractor"/> resolves one IInteractable per collider, so Ông Bear
/// carries the shop only — his recycling side quest lives on its own counter beside him.</para>
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShopNPC : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    [SerializeField] GameObject prompt;
    [Header("Shop")]
    [SerializeField] ShopController shop;

    [Header("Flavor (M7)")]
    [Tooltip("Greeting shown the first time Greenie talks to Mr. Bear, before the shop opens. " +
             "Needs a DialogueRunner in the scene; if none, the shop just opens.")]
    [SerializeField] DialogueLine[] greetingLines;

    bool greeted;

    public bool CanInteract => (shop == null || !shop.IsOpen) && !DialogueRunner.IsActive;
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

    public void Interact(GameObject interactor)
    {
        // First talk: play Ông Bear's flavor line, then open the shop. Later talks
        // open the shop directly.
        if (!greeted && greetingLines != null && greetingLines.Length > 0 && DialogueRunner.Instance != null)
        {
            greeted = true;
            if (prompt != null) prompt.SetActive(false);
            DialogueRunner.Instance.Begin(greetingLines, OpenShop);
            return;
        }
        OpenShop();
    }

    void OpenShop()
    {
        if (shop != null) shop.Open();
    }
}
