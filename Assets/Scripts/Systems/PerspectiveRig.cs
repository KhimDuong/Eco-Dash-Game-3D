using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// B6: the <b>P</b> key, and everything that has to be true while first person is live.
/// Sits on the CameraRig next to <see cref="CameraFollow"/>, which keeps owning the ¾ framing —
/// this component owns the second framing, the look input, and the four side effects a
/// perspective swap drags behind it (priority, Greenie's renderers, the cursor, the reticle).
///
/// <para><b>The first-person camera is built here, not authored in the prefab.</b> It is a
/// second <see cref="CinemachineCamera"/> whose priority is raised on toggle, which is the
/// clean Cinemachine way round: the brain blends between the two framings for free, so the
/// switch is a short dive to Greenie's eyes rather than a cut. Building it in code means the
/// eye height and the FOV cannot drift out of sync with this file, and every scene that already
/// has a CameraRig gets first person with no prefab surgery.</para>
///
/// <para><b>Greenie is hidden by his renderers, never by his Visual node.</b>
/// <see cref="PlayerAnimator"/> caches <c>baseLocalPos</c>/<c>baseScale</c> in <c>Awake</c> and
/// rewrites <c>visual.localPosition</c> every frame; deactivating that node and switching it
/// back on is the documented way to lose Greenie's rest pose (CLAUDE.md rule 2's ownership
/// trap). Toggling <c>Renderer.enabled</c> touches nothing the animator or the colliders own.</para>
///
/// <para><b>B9: the eye offset follows Greenie's own up.</b> On a wall the first-person camera
/// sits out from the rock along its normal and rolls with <see cref="PerspectiveMode.LookRotation"/>,
/// so the face he is standing on reads as the floor — the ant's answer to the design question
/// the backlog raised. The offset has to be pushed every frame rather than once at build time,
/// because it is now a value that changes; on the ground it is the authored
/// <c>Vector3.up * eyeHeight</c> and nothing moves.</para>
///
/// <para><b>The three-quarter camera is deliberately not touched.</b> Rolling it would break the
/// fixed framing golden rule #1 protects, and rolling the whole valley around a climbing robot
/// reads as the world tipping over. Greenie simply rises within the frame, which is what B8's
/// slow vertical damping on <see cref="CameraFollow"/> was already tuned for.</para>
///
/// <para><b>The cursor is a shared resource.</b> It is locked only while first person is live
/// <i>and</i> no screen is up — <see cref="UiModal"/> is what makes opening the bag mid-look
/// give the mouse back instead of stranding the player with an invisible pointer. It is also
/// released in <c>OnDisable</c>, or quitting play mode in first person leaves the editor
/// without a cursor.</para>
/// </summary>
public class PerspectiveRig : MonoBehaviour
{
    [Header("First-person framing")]
    [Tooltip("Eye height above Greenie's feet, in metres. His CharacterController is 1.15 tall.")]
    [SerializeField] float eyeHeight = 1.05f;
    [Tooltip("Field of view for first person. Wider than the 3/4 camera's 60 degrees — a narrow " +
             "FOV at eye height reads as a telephoto lens.")]
    [SerializeField] float fieldOfView = 70f;
    [Tooltip("Degrees of look per pixel of mouse movement.")]
    [SerializeField] float lookSensitivity = 0.12f;

    [Header("Transition")]
    [Tooltip("Seconds the Cinemachine brain takes to dive between the two framings. Short on " +
             "purpose: WASD switches frame the instant P is pressed, so a long blend would " +
             "leave the controls camera-relative to a camera that is still overhead.")]
    [SerializeField] float blendTime = 0.3f;
    [Tooltip("Priority the first-person camera claims. CM_PlayerCam sits at 10.")]
    [SerializeField] int firstPersonPriority = 20;

    public static PerspectiveRig Instance { get; private set; }

    CinemachineCamera fpCam;
    CinemachineFollow fpFollow;
    CinemachineBrain brain;
    Transform player;
    Renderer[] body;
    PerspectiveMode.View applied;
    bool built;

    float shakeEndTime;
    float shakeMagnitude;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Shake(float duration, float magnitude)
    {
        shakeEndTime = Time.time + duration;
        shakeMagnitude = magnitude;
    }

    void Start()
    {
        Build();
        Apply(force: true);
    }

    void OnDisable()
    {
        ReleaseCursor();
        SetBodyVisible(true);   // the next scene's rig re-hides it if first person is still on
    }

    void Update()
    {
        if (!built) Build();
        if (player == null) BindPlayer();

        var kb = Keyboard.current;
        if (kb != null && kb.pKey.wasPressedThisFrame && !UiModal.AnyOpen)
            PerspectiveMode.Toggle();

        ReadLook();
        Apply(force: false);

        Vector3 shakePosOffset = Vector3.zero;
        Quaternion shakeRotOffset = Quaternion.identity;
        if (Time.time < shakeEndTime)
        {
            float p = (shakeEndTime - Time.time);
            float m = shakeMagnitude * p;
            Vector2 r = Random.insideUnitCircle * m;
            shakePosOffset = new Vector3(r.x, r.y, 0f);
            shakeRotOffset = Quaternion.Euler(
                Random.Range(-m * 15f, m * 15f),
                Random.Range(-m * 15f, m * 15f),
                Random.Range(-m * 10f, m * 10f));
        }

        // Push the look angles every frame, not only on change: the brain reads this vcam's own
        // transform rotation (there is no Rotation Control behaviour on it, exactly as on
        // CM_PlayerCam), and it has to be correct on the first frame of the blend.
        if (fpCam != null) fpCam.transform.rotation = PerspectiveMode.LookRotation * shakeRotOffset;
        // B9: the eye rides out along whichever way is up for Greenie right now.
        if (fpFollow != null) fpFollow.FollowOffset = SurfaceFrame.VisualUp * eyeHeight + shakePosOffset;

        ApplyCursor();
    }

    // --- Setup --------------------------------------------------------------

    void Build()
    {
        if (built) return;
        built = true;

        brain = GetComponentInChildren<CinemachineBrain>();
        if (brain != null)
        {
            // Mutate the authored blend rather than replacing it: the prefab ships a 2 s
            // EaseInOut, which is a fine default for a cutscene and far too slow for a toggle.
            var blend = brain.DefaultBlend;
            blend.Time = blendTime;
            brain.DefaultBlend = blend;
        }

        BindPlayer();
        BuildFirstPersonCamera();
        FirstPersonReticle.Ensure();
    }

    void BindPlayer()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go == null) return;
        player = go.transform;
        body = go.GetComponentsInChildren<Renderer>(true);
        if (fpCam != null) fpCam.Follow = player;
        SetBodyVisible(!PerspectiveMode.IsFirstPerson);
    }

    void BuildFirstPersonCamera()
    {
        if (fpCam != null) return;

        var go = new GameObject("CM_FirstPersonCam");
        go.transform.SetParent(transform, false);

        fpCam = go.AddComponent<CinemachineCamera>();
        go.AddComponent<CinemachineImpulseListener>();
        var lens = fpCam.Lens;
        lens.FieldOfView = fieldOfView;
        lens.NearClipPlane = 0.05f;   // the eye sits inside Greenie's own capsule
        fpCam.Lens = lens;

        fpFollow = go.AddComponent<CinemachineFollow>();
        fpFollow.TrackerSettings.BindingMode = BindingMode.WorldSpace;
        fpFollow.TrackerSettings.PositionDamping = Vector3.zero; // damping at eye height is nausea
        fpFollow.FollowOffset = Vector3.up * eyeHeight;

        fpCam.Follow = player;
        SetPriority(0);
    }

    // --- Per-frame ----------------------------------------------------------

    void ReadLook()
    {
        if (!PerspectiveMode.IsFirstPerson || UiModal.AnyOpen) return;
        var mouse = Mouse.current;
        if (mouse == null) return;

        // Mouse delta is already per-frame pixels — multiplying by deltaTime would make the
        // look speed depend on the frame rate twice over.
        Vector2 d = mouse.delta.ReadValue();
        PerspectiveMode.Look(d.x * lookSensitivity, d.y * lookSensitivity);
    }

    void Apply(bool force)
    {
        if (!force && applied == PerspectiveMode.Current) return;
        applied = PerspectiveMode.Current;

        bool fp = PerspectiveMode.IsFirstPerson;
        if (fpCam != null && fpCam.Follow == null && player != null) fpCam.Follow = player;
        SetPriority(fp ? firstPersonPriority : 0);
        SetBodyVisible(!fp);
    }

    void SetPriority(int value)
    {
        if (fpCam == null) return;
        // Read-modify-write through a local: Priority is a settings struct, and this is the form
        // that compiles whether Cinemachine exposes it as a field or as a property.
        var priority = fpCam.Priority;
        priority.Value = value;
        fpCam.Priority = priority;
    }

    void SetBodyVisible(bool visible)
    {
        if (body == null) return;
        foreach (var r in body)
            if (r != null) r.enabled = visible;
    }

    void ApplyCursor()
    {
        bool locked = PerspectiveMode.IsFirstPerson && !UiModal.AnyOpen && Application.isFocused;
        if (locked)
        {
            if (Cursor.lockState == CursorLockMode.Locked) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else ReleaseCursor();
    }

    static void ReleaseCursor()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
