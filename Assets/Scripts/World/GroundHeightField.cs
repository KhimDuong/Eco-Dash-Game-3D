using UnityEngine;

/// <summary>
/// Publishes one scene's <see cref="GroundProfile"/> to <see cref="GroundHeight"/>, so that
/// gameplay can ask where the ground is without raycasting for it. Written by the level
/// generator; there is exactly one in <c>Level1_BarrenFarm</c> and none anywhere else.
/// </summary>
[DisallowMultipleComponent]
public class GroundHeightField : MonoBehaviour
{
    [SerializeField] GroundProfile profile = new();

    /// <summary>The field this scene was generated against.</summary>
    public GroundProfile Profile => profile;

    /// <summary>
    /// Claimed in <c>OnEnable</c>, deliberately. <c>Awake</c> is not dependable on this project
    /// — Fast Enter Play Mode reuses the scene's objects rather than reloading them, which is
    /// how <c>CameraFollow.Instance</c> once stayed null for a whole play session (CLAUDE.md
    /// rule 5). <c>OnEnable</c> always runs on the play-mode transition.
    /// </summary>
    void OnEnable() => GroundHeight.Profile = profile;

    /// <summary>
    /// Hand the field back on the way out, so nothing carries Level 1's hills into the flat
    /// factory. Guarded because scene loads overlap: the next scene's field may already have
    /// claimed the slot by the time this one is torn down.
    /// </summary>
    void OnDisable()
    {
        if (ReferenceEquals(GroundHeight.Profile, profile)) GroundHeight.Profile = null;
    }

    /// <summary>Generator-only. Nothing at runtime should ever reshape the ground.</summary>
    public void Author(GroundProfile authored) => profile = authored;
}
