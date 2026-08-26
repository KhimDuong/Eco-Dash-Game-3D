using System.Collections;
using UnityEngine;

/// <summary>
/// The valley healing in a patch: dead/barren soil that turns lush green when its
/// chest's core is collected — Level 1's payoff beat.
///
/// <para><b>Tier 2 (redesigned for 3D).</b> The 2D version crossfaded two layers of
/// sprite tiles ("dead*" dirt out, grass in) with a per-tile ripple delay. Sprites
/// carry alpha; opaque URP meshes don't, so the 3D patch instead does what the task
/// brief asks for — <i>swap materials in a radius</i>. The patch is a flat disc lying
/// on the ground: on <see cref="Reveal"/> it grows from nothing to
/// <see cref="radius"/> while its colour lerps barren → lush, so the green still
/// ripples outward from the centre.</para>
///
/// <para><b>The disc carries the beat on its own.</b> The patch could also re-tint the
/// ground renderers the wave sweeps over (<see cref="tintSurroundings"/>), and did until
/// QA C5: the floor's smallest repaintable unit is a whole 4 m tile, so a 3.5 m circle
/// turned up to eight 16 m² squares solid green and the valley's payoff beat rendered as
/// a Tetris shape with 90° corners — 432 m² of green from four discs totalling 154 m².
/// The decal is already a circle, already animates outward and already lands in the right
/// place, so it is left to do the job alone and its radius was raised to compensate.
/// Tinting the terrain itself needs the ground to stop being 192 flat tiles, which is the
/// same structural change undulating terrain would need; the two should be costed
/// together.</para>
/// </summary>
public class ReclamationPatch : MonoBehaviour
{
    [Header("Reveal")]
    [Tooltip("Final radius of the reclaimed disc, in metres.")]
    [SerializeField] float radius = 4.5f;
    [Tooltip("Seconds for the wave to travel from the centre to the rim.")]
    [SerializeField] float revealDuration = 0.9f;

    [Header("Look")]
    [SerializeField] Color barrenColor = new Color(0.42f, 0.34f, 0.24f);
    [SerializeField] Color lushColor = new Color(0.30f, 0.66f, 0.28f);
    [Tooltip("Disc mesh lying on the ground. Auto-found among the children if empty.")]
    [SerializeField] Renderer discRenderer;

    [Header("Spread to the ground around it")]
    [Tooltip("Also re-tint whole ground tiles the wave sweeps over. Off: the floor is 192 flat " +
             "4 m tiles and one renderer is the smallest thing this can repaint, so the round " +
             "wave comes out as a Tetris shape of hard-edged green squares (QA C5).")]
    [SerializeField] bool tintSurroundings;
    [SerializeField] LayerMask surroundingsMask = ~0;

    bool revealed;
    Transform disc;

    /// <summary>True once the patch has bloomed — read by save/restore paths.</summary>
    public bool Revealed => revealed;

    void Awake()
    {
        if (discRenderer == null) discRenderer = GetComponentInChildren<Renderer>(true);
        if (discRenderer != null)
        {
            disc = discRenderer.transform;
            disc.localScale = new Vector3(0f, disc.localScale.y, 0f);   // nothing to see yet
            MaterialTint.Apply(discRenderer, barrenColor);
        }
    }

    public void Reveal()
    {
        if (revealed) return;
        revealed = true;
        if (isActiveAndEnabled) StartCoroutine(RevealRoutine());
        else ApplyFinalState();
    }

    /// <summary>Snap straight to the bloomed look (revisiting a level, or no coroutine host).</summary>
    public void ApplyFinalState()
    {
        revealed = true;
        if (disc != null) disc.localScale = new Vector3(radius * 2f, disc.localScale.y, radius * 2f);
        MaterialTint.Apply(discRenderer, lushColor);
        if (tintSurroundings) TintWithin(radius);
    }

    IEnumerator RevealRoutine()
    {
        float t = 0f;
        float tinted = 0f;
        while (t < revealDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / revealDuration);
            float eased = Mathf.SmoothStep(0f, 1f, u);

            if (disc != null)
            {
                float d = radius * 2f * eased;
                disc.localScale = new Vector3(d, disc.localScale.y, d);
            }
            MaterialTint.Apply(discRenderer, Color.Lerp(barrenColor, lushColor, eased));

            // Let the wave front carry the ground tint outward with it.
            if (tintSurroundings)
            {
                float front = radius * eased;
                if (front - tinted > 0.35f) { TintWithin(front); tinted = front; }
            }
            yield return null;
        }
        ApplyFinalState();
    }

    void TintWithin(float r)
    {
        var hits = Physics.OverlapSphere(transform.position, r, surroundingsMask, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            // Only ground: props, the player and pickups keep their own look.
            if (!h.gameObject.CompareTag("Untagged")) continue;
            if (h.GetComponentInParent<ReclamationPatch>() != null) continue;
            if (!h.TryGetComponent<Renderer>(out var r2)) continue;
            if (r2.gameObject.layer != LayerMask.NameToLayer("Ground")) continue;
            MaterialTint.Apply(r2, lushColor);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(lushColor.r, lushColor.g, lushColor.b, 0.5f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
