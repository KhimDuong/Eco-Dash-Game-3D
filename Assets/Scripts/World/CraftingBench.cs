using UnityEngine;

/// <summary>
/// Bàn Chế Tạo (crafting bench) world object at the hub (M9, K4). Press <b>E</b> to
/// open the <see cref="CraftingUI"/>. Mirrors the <see cref="Chest"/>/<see cref="Keycard"/>
/// <see cref="IInteractable"/> contract. The UI is created on demand if the scene
/// doesn't already have one, so the bench works dropped into any scene.
///
/// <para>3D port (Tier 1): <c>Collider2D</c> → <see cref="Collider"/>. <see cref="CraftingUI"/>
/// builds itself through <c>UIFactory</c>, so nothing about the window needed porting.</para>
/// </summary>
[RequireComponent(typeof(Collider))]
public class CraftingBench : MonoBehaviour, IInteractable
{
    [Tooltip("\"Nhấn E\" prompt shown while Greenie is the nearest interactable.")]
    [SerializeField] GameObject prompt;
    [SerializeField] AudioClip openSfx;

    CraftingUI ui;

    public bool CanInteract => !DialogueRunner.IsActive && (ui == null || !ui.IsOpen);
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
        EnsureUI();
        if (prompt != null) prompt.SetActive(false);
        if (openSfx != null) AudioSource.PlayClipAtPoint(openSfx, transform.position);
        ui.Open();
    }

    void EnsureUI()
    {
        if (ui != null) return;
        ui = FindFirstObjectByType<CraftingUI>(FindObjectsInactive.Include);
        if (ui == null) ui = new GameObject("CraftingUI").AddComponent<CraftingUI>();
    }
}
