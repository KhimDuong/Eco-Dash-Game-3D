using UnityEngine;

/// <summary>
/// Fixed ¾ top-down camera that trails the player (Tunic / Death's Door framing).
/// Assign the player as the target, or it auto-finds the object tagged "Player".
/// The player never controls the camera — pitch and distance are authored, not input.
///
/// A4 replaces the *follow* half of this with a Cinemachine camera; <see cref="Shake"/>
/// and <see cref="Instance"/> are the contract that ported callers (PlayerHealth, the
/// bosses) rely on, so they keep working when the follow is swapped for an impulse
/// source. Don't change those two signatures.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    /// <summary>Set in Awake so gameplay code (e.g. PlayerHealth) can request a shake.</summary>
    public static CameraFollow Instance { get; private set; }

    [Header("Target")]
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
    [SerializeField] float smoothTime = 0.15f;

    Vector3 velocity;
    float shakeTimeLeft;
    float shakeDuration;
    float shakeMagnitude;

    void Awake() => Instance = this;
    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
        transform.rotation = Rotation;
        if (target != null) transform.position = DesiredPosition;   // no lerp-in on the first frame
    }

    Quaternion Rotation => Quaternion.Euler(pitch, yaw, 0f);

    // Sit back along the view direction: pitch 50°/distance 12 puts the camera
    // ~9.2 m up and ~7.7 m behind the target.
    Vector3 DesiredPosition =>
        target.position + Vector3.up * targetHeightOffset + Rotation * Vector3.back * distance;

    /// <summary>Kick off a brief positional shake (e.g. on player damage).</summary>
    public void Shake(float duration, float magnitude)
    {
        // Keep the strongest pending shake rather than cutting one short.
        shakeMagnitude = Mathf.Max(shakeMagnitude * Mathf.Clamp01(shakeTimeLeft / Mathf.Max(shakeDuration, 0.0001f)), magnitude);
        shakeDuration = duration;
        shakeTimeLeft = duration;
    }

    void LateUpdate()
    {
        if (target == null) return;

        transform.rotation = Rotation;
        transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition, ref velocity, smoothTime);

        if (shakeTimeLeft > 0f)
        {
            shakeTimeLeft -= Time.unscaledDeltaTime;
            float falloff = Mathf.Clamp01(shakeTimeLeft / Mathf.Max(shakeDuration, 0.0001f));
            Vector2 jitter = Random.insideUnitCircle * (shakeMagnitude * falloff);
            // Jitter across the screen plane, not the world plane, so it reads the
            // same as the 2D shake regardless of the camera's tilt.
            transform.position += transform.right * jitter.x + transform.up * jitter.y;
        }
    }
}
