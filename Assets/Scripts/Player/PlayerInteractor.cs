using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads the E key and lets Greenie act on the nearest <see cref="IInteractable"/>
/// in range (chests, NPCs). Highlights the current target each frame so its prompt
/// shows. Uses direct keyboard polling to match PlayerController / PlayerShooter
/// (the design contract: E = interact).
///
/// 3D port note: Physics2D.OverlapCircle becomes Physics.OverlapSphereNonAlloc,
/// still including triggers and ignoring layers — targets are discriminated by the
/// IInteractable component, exactly as in 2D.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("How close Greenie must be (world units) to act on something.")]
    [SerializeField] float interactRadius = 1.8f;

    // Interactables sit on trigger colliders, so the query must include triggers.
    const int MaxOverlaps = 32;
    readonly Collider[] hits = new Collider[MaxOverlaps];
    IInteractable current;

    void Update()
    {
        // While a dialogue line is up, drop the current target (hide its prompt) and
        // don't read E — the runner owns E to advance, and this also swallows the
        // dismissing keypress so we don't instantly re-open the same NPC (M7).
        if (DialogueRunner.IsActive)
        {
            if (current != null) { current.SetHighlighted(false); current = null; }
            return;
        }

        IInteractable nearest = FindNearest();
        if (!ReferenceEquals(nearest, current))
        {
            current?.SetHighlighted(false);
            current = nearest;
            current?.SetHighlighted(true);
        }

        var kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame && current != null && current.CanInteract)
            current.Interact(gameObject);
    }

    IInteractable FindNearest()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, interactRadius, hits, ~0, QueryTriggerInteraction.Collide);

        IInteractable best = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (!hits[i].TryGetComponent<IInteractable>(out var it) || !it.CanInteract) continue;
            float sqr = (it.InteractPosition - transform.position).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = it; }
        }
        return best;
    }

    void OnDisable() => current?.SetHighlighted(false);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
