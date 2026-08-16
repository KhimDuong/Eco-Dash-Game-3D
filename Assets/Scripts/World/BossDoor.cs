using System.Collections;
using UnityEngine;

/// <summary>
/// Cửa Boss (boss blast door) sealing the Mega-Smog arena. Locked until every
/// keycard is collected (<see cref="GameManager.OnAllCoresCollected"/> — the same
/// hook Level 1's teleport gate uses), then the two panels retract and the solid
/// blocker drops so Greenie can enter. Opening the door does NOT win the level;
/// the boss must be defeated for that (it calls <see cref="GameManager.CompleteLevel"/>).
///
/// <para>3D port (Tier 1): <c>Collider2D</c> → <see cref="Collider"/> and the status
/// lights tint through <see cref="MaterialTint"/> instead of <c>SpriteRenderer.color</c>.
/// The panels still retract along ±X, which is correct here — the door spans the
/// corridor on X and Greenie walks through it heading north.</para>
/// </summary>
public class BossDoor : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Solid blocker collider, disabled when the door opens.")]
    [SerializeField] Collider blocker;
    [SerializeField] Transform leftPanel;
    [SerializeField] Transform rightPanel;

    [Header("Open motion")]
    [Tooltip("How far each panel slides apart.")]
    [SerializeField] float retract = 1.0f;
    [SerializeField] float openDuration = 0.8f;

    [Header("Status lights (red locked → green open)")]
    [SerializeField] Renderer[] lights;
    [SerializeField] Color lockedColor = new Color(0.9f, 0.2f, 0.2f, 1f);
    [SerializeField] Color openColor = new Color(0.3f, 1f, 0.4f, 1f);

    [Header("Audio")]
    [SerializeField] AudioClip openSfx;

    bool open;
    Vector3 leftBase, rightBase;

    /// <summary>True once the keycards are in and the panels have been told to part.</summary>
    public bool IsOpen => open;

    void Awake()
    {
        if (leftPanel != null) leftBase = leftPanel.localPosition;
        if (rightPanel != null) rightBase = rightPanel.localPosition;
        SetLights(lockedColor);
    }

    void Start()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnAllCoresCollected += Open;
        if (GameManager.Instance.AllCoresCollected) Open(); // safety on re-entry
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnAllCoresCollected -= Open;
    }

    public void Open()
    {
        if (open) return;
        open = true;
        SetLights(openColor);
        if (openSfx != null) Sfx.Play(openSfx, transform.position);
        Debug.Log("[Eco-Dash] Boss door unlocked — the Mega-Smog awaits.");
        StartCoroutine(OpenRoutine());
    }

    IEnumerator OpenRoutine()
    {
        Vector3 lTo = leftBase + Vector3.left * retract;
        Vector3 rTo = rightBase + Vector3.right * retract;
        float t = 0f;
        while (t < openDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, t / openDuration);
            if (leftPanel != null) leftPanel.localPosition = Vector3.Lerp(leftBase, lTo, u);
            if (rightPanel != null) rightPanel.localPosition = Vector3.Lerp(rightBase, rTo, u);
            yield return null;
        }
        if (blocker != null) blocker.enabled = false; // drop the wall after the panels part
    }

    void SetLights(Color c)
    {
        if (lights == null) return;
        foreach (var l in lights) MaterialTint.Apply(l, c);
    }
}
