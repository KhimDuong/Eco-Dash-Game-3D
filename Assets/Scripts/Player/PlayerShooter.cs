using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Fires Seed projectiles (Hạt mầm) in the direction Greenie is facing.
/// Press J; rate-limited by fireCooldown. The aim comes from PlayerController.
///
/// 3D port note: the spread fan is unchanged, just swung around the world Y axis
/// so it fans out across the ground plane instead of the screen plane.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerShooter : MonoBehaviour
{
    [Header("Shooting")]
    [SerializeField] GameObject seedPrefab;
    [Tooltip("Spawn origin for seeds. Defaults to this transform if unset.")]
    [SerializeField] Transform firePoint;
    [SerializeField] float fireCooldown = 0.35f;
    [Tooltip("Degrees between seeds when the spread upgrade fires a fan.")]
    [SerializeField] float spreadAngle = 12f;

    [Header("Audio (optional)")]
    [SerializeField] AudioClip shootSfx;

    PlayerController controller;
    AudioSource audioSource;
    float nextFireTime;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        audioSource = GetComponent<AudioSource>();
        if (firePoint == null) firePoint = transform;
    }

    void Update()
    {
        if (DialogueRunner.IsActive) return; // no shooting mid-dialogue (M7)

        var kb = Keyboard.current;
        if (kb == null || seedPrefab == null) return;

        // Hold-to-fire with cooldown so it feels responsive but not spammy.
        if (kb.jKey.isPressed && Time.time >= nextFireTime)
            Fire();
    }

    void Fire()
    {
        nextFireTime = Time.time + fireCooldown;

        // Seed count comes from the permanent spread upgrade (1 / 3 / 5 / 7),
        // fired as a symmetric fan centred on the facing direction.
        int count = Mathf.Max(1, PlayerProgress.SeedCount);
        Vector3 facing = controller.FacingDirection;
        // Yaw of the facing direction: Atan2(x, z) is the Y-rotation that maps
        // Vector3.forward onto it.
        float baseAngle = Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg;
        float startOffset = -(count - 1) * 0.5f * spreadAngle;

        for (int i = 0; i < count; i++)
        {
            Quaternion rotation = Quaternion.Euler(0f, baseAngle + startOffset + i * spreadAngle, 0f);
            Vector3 dir = rotation * Vector3.forward;
            GameObject seed = Instantiate(seedPrefab, firePoint.position, rotation);
            if (seed.TryGetComponent<SeedProjectile>(out var projectile))
                projectile.Launch(dir);
        }

        if (shootSfx != null && audioSource != null)
            audioSource.PlayOneShot(shootSfx);
    }
}
