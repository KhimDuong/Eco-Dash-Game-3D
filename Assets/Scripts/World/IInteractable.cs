using UnityEngine;

/// <summary>
/// Something Greenie can act on with the E key — chests now, NPCs/levers later.
/// <see cref="PlayerInteractor"/> finds the nearest one in range, highlights it,
/// and drives <see cref="Interact"/> when the player presses E.
/// </summary>
public interface IInteractable
{
    /// <summary>False once it's spent (e.g. an opened chest) so it stops being targeted.</summary>
    bool CanInteract { get; }

    /// <summary>World position used to pick the nearest interactable.</summary>
    Vector3 InteractPosition { get; }

    /// <summary>Show/hide the "Nhấn E" prompt as the player's nearest target changes.</summary>
    void SetHighlighted(bool highlighted);

    /// <summary>Do the thing. <paramref name="interactor"/> is the player GameObject.</summary>
    void Interact(GameObject interactor);
}
