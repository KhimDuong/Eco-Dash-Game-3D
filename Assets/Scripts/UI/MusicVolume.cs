using UnityEngine;

/// <summary>
/// Attach to a background-music <see cref="AudioSource"/>. Scales the source's
/// volume by <see cref="GameSettings.EffectiveMusic"/> so the in-game Music
/// slider controls the soundtrack independently of SFX. The volume authored on
/// the source in the editor is treated as the 100% reference.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicVolume : MonoBehaviour
{
    AudioSource source;
    float baseVolume;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        baseVolume = source.volume;
    }

    void OnEnable()
    {
        GameSettings.OnChanged += Apply;
        Apply();
    }

    void OnDisable() => GameSettings.OnChanged -= Apply;

    void Apply()
    {
        if (source != null) source.volume = baseVolume * GameSettings.EffectiveMusic;
    }
}
