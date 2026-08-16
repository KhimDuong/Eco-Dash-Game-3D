using UnityEngine;

/// <summary>
/// C5's one-shot sound service — the single route every sound effect in the game takes.
/// A service rather than a per-caller <see cref="AudioSource"/>, for the same reason
/// <see cref="GameFeel"/> is one: the sound has to outlive the thing that made it, and a
/// slime's own AudioSource dies with the slime on the very frame its death sound starts.
///
/// <para><b>Why not <c>AudioSource.PlayClipAtPoint</c>, which every ported caller used?</b>
/// Because it builds a <i>3D</i> sound — measured in play mode: <c>spatialBlend = 1</c>,
/// logarithmic rolloff, <c>minDistance = 1</c>. In the 2D game that was survivable; here the
/// <see cref="AudioListener"/> rides the camera, and the ¾ rig parks the camera <b>12.4 m</b>
/// behind and above Greenie. Logarithmic rolloff at 12.4 m is a gain of about <b>0.08</b>, so
/// every sound in the game would play at 8% of its authored volume — quiet enough to read as
/// "the audio is broken" rather than "the audio is distant". The 2D build had already met a
/// milder version of this and patched two call sites by hand
/// (<see cref="QuestItemPickup"/> and <see cref="EndScreenController"/> both play at
/// <c>Camera.main.position</c> so the listener is standing on top of the sound); C5 fixes the
/// cause instead of adding a third patch.</para>
///
/// <para><b>Sounds are 2D, attenuated by distance from Greenie.</b> Under a fixed ¾ camera
/// there is nothing for real 3D audio to tell the player: the listener sits at a constant
/// offset, so a slime 10 m away is 15.9 m from the camera against 12.4 m for one at his feet —
/// a difference the ear cannot use. Worse, moving the listener onto Greenie to fix that would
/// make the stereo image <i>rotate with him</i>, because his visual child turns to face
/// travel; a sound on the left of the screen would pan right the moment he walked south.
/// So panning is dropped and only the useful half is kept — the distance that decides how loud
/// something is, measured from Greenie, where the player's intuition already measures it.</para>
///
/// <para>Volume still passes through <see cref="AudioListener.volume"/>, which
/// <see cref="GameSettings"/> drives from the Master slider and the mute toggle, so the
/// settings contract is unchanged from the 2D build.</para>
/// </summary>
public static class Sfx
{
    /// <summary>Within this distance of Greenie a sound plays at full volume, in metres.</summary>
    public const float NearDistance = 9f;

    /// <summary>Beyond this distance a sound is silent and never claims a voice.</summary>
    public const float FarDistance = 34f;

    // Enough voices that a busy moment (a Seed Bomb clearing four slimes) never reuses one
    // mid-play. They exist so each sound can carry its own pitch: PlayOneShot reads the
    // source's pitch when it starts, so two sounds sharing a source must share a pitch, and
    // re-pitching for the second would bend the first one already in flight.
    const int Voices = 8;

    // A hair of pitch scatter. Eight clips cover the whole game and the slime death alone
    // fires 29 times a level; identical playback is what makes that read as one sound looping
    // rather than several things dying.
    const float PitchJitter = 0.07f;

    static AudioSource[] voices;
    static int next;
    static Transform ear;

    // The jitter draws from its own generator rather than UnityEngine.Random, and that is not
    // fussiness. UnityEngine.Random is one global sequence, and gameplay is spending it:
    // PlasticSlime picks its wander target and repath timer from it, Litter and the enemies
    // roll their drops from it. Take one number per sound effect out of that sequence and
    // every one of those draws shifts — audio, of all things, quietly changing where the
    // slimes walk. Measured, not theorised: routing the jitter through the shared generator
    // moved a wandering slime from 1.8 m to 2.4 m off its spawn and broke a combat test that
    // had nothing to do with sound.
    static System.Random jitterRng = new System.Random(20260815);

    // Fast Enter Play Mode keeps the domain, so these would otherwise still point at the last
    // session's destroyed AudioSources. Same reset every static service in this project needs.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        voices = null;
        next = 0;
        ear = null;
        jitterRng = new System.Random(20260815);
    }

    /// <summary>
    /// Play a sound that happens somewhere in the world: quieter the further it is from
    /// Greenie, silent past <see cref="FarDistance"/>. The drop-in replacement for
    /// <c>AudioSource.PlayClipAtPoint(clip, pos)</c>.
    /// </summary>
    public static void Play(AudioClip clip, Vector3 at, float volume = 1f)
    {
        if (clip == null) return;
        float gain = Attenuation(at);
        if (gain <= 0.01f) return;          // inaudible — don't spend a voice on it
        Emit(clip, volume * gain);
    }

    /// <summary>
    /// Play a sound that isn't in the world at all — UI, jingles, the shop till — at full
    /// volume regardless of where Greenie is standing.
    /// </summary>
    public static void Play2D(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        Emit(clip, volume);
    }

    /// <summary>How loud a sound at this position should be, 1 next to Greenie and 0 far off.</summary>
    public static float Attenuation(Vector3 at)
    {
        var listenAt = Ear();
        if (!listenAt.HasValue) return 1f;    // no player and no camera: don't silence the game
        Vector3 d = at - listenAt.Value;
        d.y = 0f;                             // distance is XZ here, like everything else
        float dist = d.magnitude;
        if (dist <= NearDistance) return 1f;
        if (dist >= FarDistance) return 0f;
        return Mathf.SmoothStep(1f, 0f, (dist - NearDistance) / (FarDistance - NearDistance));
    }

    // Greenie is the ear. The camera stands in for him in the scenes he isn't in (the menu and
    // the two story scenes), where nothing calls Play(at) anyway but Play2D still needs a pool.
    static Vector3? Ear()
    {
        if (ear == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            ear = player != null ? player.transform : null;
        }
        if (ear != null) return ear.position;
        var cam = Camera.main;
        return cam != null ? cam.transform.position : (Vector3?)null;
    }

    static void Emit(AudioClip clip, float volume)
    {
        var source = NextVoice();
        if (source == null) return;
        source.pitch = 1f + (float)(jitterRng.NextDouble() * 2.0 - 1.0) * PitchJitter;
        source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    static AudioSource NextVoice()
    {
        if (!Application.isPlaying) return null;
        EnsurePool();
        if (voices == null) return null;
        var source = voices[next];
        next = (next + 1) % voices.Length;
        return source;
    }

    static void EnsurePool()
    {
        if (voices != null && voices.Length > 0 && voices[0] != null) return;

        var host = new GameObject("~Sfx");
        Object.DontDestroyOnLoad(host);

        voices = new AudioSource[Voices];
        for (int i = 0; i < Voices; i++)
        {
            var source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;    // the whole point: 2D, so distance is ours to decide
            source.rolloffMode = AudioRolloffMode.Linear;
            source.volume = 1f;
            voices[i] = source;
        }
        next = 0;
    }
}
