using System;
using UnityEngine;

/// <summary>
/// Player HP (thanh máu). Handles damage with brief invulnerability frames,
/// healing, death, and raises OnHealthChanged so the HUD can react.
///
/// 3D port note: the 2D sprite-tint hit flash becomes a material colour flash
/// driven through a MaterialPropertyBlock, so no material instance is leaked.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] int maxHealth = 6;
    [Tooltip("Seconds of invulnerability after taking a hit.")]
    [SerializeField] float invulnDuration = 0.8f;

    [Header("Feedback (optional)")]
    [Tooltip("Renderer flashed red on a hit. Defaults to the first renderer on the visual child.")]
    [SerializeField] Renderer rendererToFlash;
    [SerializeField] AudioClip hurtSfx;
    [SerializeField] AudioClip deathSfx;
    [Tooltip("Camera shake strength when Greenie takes a hit.")]
    [SerializeField] float hitShakeMagnitude = 0.18f;
    [SerializeField] float hitShakeDuration = 0.18f;
    [Tooltip("How long input-steering is suspended while knocked back.")]
    [SerializeField] float knockbackDuration = 0.18f;

    /// <summary>Raised with (current, max) whenever health changes.</summary>
    public event Action<int, int> OnHealthChanged;
    /// <summary>Raised once when health reaches zero.</summary>
    public event Action OnDied;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;

    /// <summary>True while a Lá Chắn Mầm (Sprout Shield) consumable is active.</summary>
    public bool IsShielded => Time.time < shieldedUntil;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP Lit / Simple Lit
    static readonly int ColorId = Shader.PropertyToID("_Color");         // unlit & legacy shaders

    float invulnUntil;
    float shieldedUntil;
    bool isDead;
    PlayerController controller;

    MaterialPropertyBlock flashBlock;
    int flashColorId;
    Color flashOriginalColor;
    bool canFlash;

    void Awake()
    {
        maxHealth += PlayerProgress.MaxHpBonus; // permanent max-HP upgrade (M4 shop)
        CurrentHealth = maxHealth;
        controller = GetComponent<PlayerController>();
        if (rendererToFlash == null) rendererToFlash = GetComponentInChildren<Renderer>();
        SetUpFlash();
    }

    void Start() => OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

    /// <summary>Plain hit (no knockback). Returns true if the hit actually landed.</summary>
    public bool TakeDamage(int amount) => TakeDamage(amount, transform.position, 0f);

    /// <summary>
    /// Hit from <paramref name="sourcePosition"/>; on a landed hit, Greenie is shoved
    /// away from the source along the ground plane and the camera shakes. Returns true
    /// if the hit landed (false while dead or during invulnerability).
    /// </summary>
    public bool TakeDamage(int amount, Vector3 sourcePosition, float knockbackForce)
    {
        if (isDead || Time.time < invulnUntil || IsShielded || amount <= 0) return false;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        invulnUntil = Time.time + invulnDuration;
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (canFlash) StartCoroutine(FlashRoutine());
        if (knockbackForce > 0f && controller != null)
        {
            Vector3 away = transform.position - sourcePosition;
            away.y = 0f;   // shove along the ground, never upward
            away = away.sqrMagnitude > 0.0001f ? away.normalized : Vector3.forward;
            controller.ApplyKnockback(away * knockbackForce, knockbackDuration);
        }
        GameFeel.Shake(hitShakeDuration, hitShakeMagnitude);

        if (CurrentHealth == 0)
        {
            isDead = true;
            if (deathSfx != null) Sfx.Play(deathSfx, transform.position);
            OnDied?.Invoke();
            if (GameManager.Instance != null) GameManager.Instance.OnPlayerDied();
        }
        else
        {
            // C4: the world lurches for a moment. Deliberately not on the killing blow —
            // GameManager.OnPlayerDied parks the clock at 0 for the lose screen, and a
            // hit-stop racing that would be invisible at best.
            GameFeel.HitStop(GameFeel.StopHurt);
            if (hurtSfx != null) Sfx.Play(hurtSfx, transform.position);
        }
        return true;
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    /// <summary>
    /// Grant a temporary damage-blocking shield (Lá Chắn Mầm consumable, M9). While
    /// active, <see cref="TakeDamage(int,Vector3,float)"/> ignores all hits. Extends
    /// rather than stacks, like the speed boost.
    /// </summary>
    public void ActivateShield(float seconds)
    {
        if (isDead || seconds <= 0f) return;
        shieldedUntil = Mathf.Max(shieldedUntil, Time.time + seconds);
    }

    // Cache which colour property this material exposes so the flash works on both
    // URP Lit (_BaseColor) and unlit placeholder materials (_Color).
    void SetUpFlash()
    {
        if (rendererToFlash == null) return;
        var mat = rendererToFlash.sharedMaterial;
        if (mat == null) return;

        if (mat.HasProperty(BaseColorId)) flashColorId = BaseColorId;
        else if (mat.HasProperty(ColorId)) flashColorId = ColorId;
        else return;

        flashOriginalColor = mat.GetColor(flashColorId);
        flashBlock = new MaterialPropertyBlock();
        canFlash = true;
    }

    System.Collections.IEnumerator FlashRoutine()
    {
        flashBlock.SetColor(flashColorId, new Color(1f, 0.4f, 0.4f, 1f));
        rendererToFlash.SetPropertyBlock(flashBlock);
        yield return new WaitForSeconds(0.1f);
        flashBlock.SetColor(flashColorId, flashOriginalColor);
        rendererToFlash.SetPropertyBlock(flashBlock);
    }
}
