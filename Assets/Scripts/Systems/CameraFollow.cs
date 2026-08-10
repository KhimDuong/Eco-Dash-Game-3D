using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

/// <summary>
/// Fixed ¾ top-down camera rig (Tunic / Death's Door framing), driven by Cinemachine.
///
/// This component lives on the <see cref="CinemachineCamera"/> and does two jobs:
/// it authors the framing (pitch / yaw / distance → the vcam's rotation and
/// <see cref="CinemachineFollow.FollowOffset"/>) and it exposes the shake API that
/// ported 2D callers still use. The player never controls the camera — pitch and
/// distance are authored, not input.
///
/// <see cref="Instance"/> and <see cref="Shake"/> are the contract ported callers
/// (PlayerHealth, the bosses) rely on. Don't change those two signatures; the shake
/// now travels through a <see cref="CinemachineImpulseSource"/> instead of moving the
/// transform directly, so it composes with any other impulse in the scene.
/// </summary>
[RequireComponent(typeof(CinemachineCamera))]
[RequireComponent(typeof(CinemachineFollow))]
[RequireComponent(typeof(CinemachineImpulseSource))]
[ExecuteAlways]
public class CameraFollow : MonoBehaviour
{
    /// <summary>Set in Awake so gameplay code (e.g. PlayerHealth) can request a shake.</summary>
    public static CameraFollow Instance { get; private set; }

    [Header("Target")]
    [Tooltip("Leave empty and the rig binds to the object tagged \"Player\" on Start.")]
    [SerializeField] Transform target;

    [Header("Framing (fixed ¾ top-down)")]
    [Tooltip("Downward tilt in degrees. ~50° is the Eco-Dash 3D house angle.")]
    [SerializeField] float pitch = 50f;
    [Tooltip("Rotation around Y. Kept at 0 so W always moves 'up' the screen.")]
    [SerializeField] float yaw = 0f;
    [Tooltip("Metres from the target along the view direction.")]
    [SerializeField] float distance = 12f;
    [Tooltip("Raise the aim point off the ground so Greenie sits low in frame.")]
    [SerializeField] float targetHeightOffset = 0.5f;

    [Header("Follow")]
    [Tooltip("Cinemachine position damping, in seconds. Matches the old SmoothDamp feel.")]
    [SerializeField] float smoothTime = 0.15f;

    [Header("Shake")]
    [Tooltip("Impulse velocity (m/s) generated per 1 unit of Shake magnitude. Measured in " +
             "play mode: a Bump impulse displaces the camera by ~1 m per 1 m/s, so 1.0 keeps " +
             "the 2D contract that magnitude is the peak offset in metres.")]
    [SerializeField] float impulseVelocityPerUnit = 1f;

    CinemachineCamera vcam;
    CinemachineFollow follow;
    CinemachineImpulseSource impulse;

    Quaternion Rotation => Quaternion.Euler(pitch, yaw, 0f);

    /// <summary>
    /// Where the camera sits relative to the target. Pitch 50° / distance 12 puts it
    /// ~9.2 m up and ~7.7 m behind, plus the aim-point lift.
    /// </summary>
    public Vector3 FollowOffset =>
        Vector3.up * targetHeightOffset + Rotation * Vector3.back * distance;

    void Awake()
    {
        Cache();
        if (Application.isPlaying) Instance = this;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Cache()
    {
        if (vcam == null) vcam = GetComponent<CinemachineCamera>();
        if (follow == null) follow = GetComponent<CinemachineFollow>();
        if (impulse == null) impulse = GetComponent<CinemachineImpulseSource>();
    }

    void Start()
    {
        if (target == null && Application.isPlaying)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
        ApplyFraming();
        // Snap on the first frame instead of sliding in from wherever the rig was authored.
        if (Application.isPlaying && target != null)
            vcam.ForceCameraPosition(target.position + FollowOffset, Rotation);
    }

    void OnValidate()
    {
        Cache();
        ApplyFraming();
    }

    /// <summary>Push the authored framing onto the Cinemachine camera. Safe to call any time.</summary>
    public void ApplyFraming()
    {
        if (vcam == null) return;
        // No Rotation Control behaviour on the vcam: its own transform rotation *is* the
        // camera rotation, which is exactly the "fixed angle, no player control" rule.
        transform.rotation = Rotation;
        if (target != null) vcam.Follow = target;
        if (follow != null)
        {
            follow.FollowOffset = FollowOffset;
            follow.TrackerSettings.BindingMode = BindingMode.WorldSpace;
            follow.TrackerSettings.PositionDamping = Vector3.one * smoothTime;
        }
    }

    /// <summary>Kick off a brief shake (e.g. on player damage). 2D-parity API.</summary>
    public void Shake(float duration, float magnitude)
    {
        if (impulse == null || magnitude <= 0f) return;
        impulse.ImpulseDefinition.ImpulseDuration = Mathf.Max(duration, 0.01f);

        // Jitter across the screen plane, not the world plane, so it reads the same as
        // the 2D shake regardless of the camera's tilt. The listener is in camera space,
        // so a local X/Y velocity is all we need.
        Vector2 dir = Random.insideUnitCircle;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        dir.Normalize();
        impulse.GenerateImpulseWithVelocity(
            new Vector3(dir.x, dir.y, 0f) * (magnitude * impulseVelocityPerUnit));
    }
}
