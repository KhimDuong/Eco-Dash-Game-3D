using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The cleaning loop (M9 §4.7.5): clearing a piece of trash cleans the ground around it,
/// raises that stage's <b>Độ Sạch</b> (<see cref="Codex.AddCleanliness"/>) and pays out at
/// 50% and 100%. The design doc has specified this as <c>GroundCleanser.CleanRadius(pos, r)</c>
/// since M9 and both the 2D <see cref="Codex"/> and <see cref="ItemUse"/> carry comments
/// pointing at it, but <b>it was never written</b> — in the 2D build nothing ever called
/// <c>AddCleanliness</c>, so the codex's third tab showed two bars frozen at 0% for the whole
/// game. C4 is where it lands, because the cleanse is as much a game-feel beat as a system.
///
/// <para><b>The share per piece is derived, not authored.</b> Each stage's meter is
/// "how much of this stage's trash have you cleared", so the percentage is recomputed from
/// the count every time rather than accumulated — <c>100 × cleaned / authored</c>, with only
/// the difference handed to the codex. Accumulating a per-piece share instead would repeat
/// C3's enrage bug in a new costume: seven pieces at 100/7 each sums to 99.99999, and a meter
/// that stops one ten-thousandth short of 100 never pays out its Portal Shard.</para>
///
/// <para><b>Counting happens in Awake, deliberately.</b> A <see cref="Litter"/> already
/// cleaned on an earlier visit deletes itself in <c>Start</c>, so a count taken any later
/// would see only what is left and inflate every remaining piece's share. Every Litter
/// registers itself in its own <c>Awake</c> — before any <c>Start</c> runs — so the total is
/// the <em>authored</em> one whether the player is arriving fresh or coming back with half
/// the field already clear. The deleted ones report themselves as already-cleaned on the way
/// out, which both keeps the running count honest and repaints their patch of ground, so a
/// revisited stage still looks cleaned rather than resetting to bare dirt.</para>
/// </summary>
public static class GroundCleanser
{
    /// <summary>Ground colour a cleansed patch settles on, per stage.</summary>
    static readonly Color ValleyClean = new Color(0.34f, 0.60f, 0.30f);   // grass returning
    static readonly Color FactoryClean = new Color(0.52f, 0.55f, 0.58f);  // scrubbed concrete

    /// <summary>How far around a cleared piece of trash the ground turns clean, in metres.</summary>
    public const float PieceRadius = 1.6f;

    static readonly List<Litter> live = new List<Litter>();
    static string trackedScene;
    static int lastRegisterFrame = int.MinValue;
    static int authored;
    static int cleaned;

    /// <summary>Authored trash count for the current stage — what 100% is measured against.</summary>
    public static int Authored => authored;
    /// <summary>How many pieces are accounted for, including ones cleared on an earlier visit.</summary>
    public static int Cleaned => cleaned;

    /// <summary>
    /// Wipe the tally when Play is pressed. The project runs with Fast Enter Play Mode, so the
    /// domain is <b>not</b> reloaded between sessions and static state survives from the last
    /// run — a second Play on the same level otherwise starts at "16 authored, 8 cleaned" and
    /// every piece is worth half. The stores this sits beside (<see cref="Codex"/>,
    /// <see cref="SceneProgress"/>) are immune by construction because they re-read themselves
    /// from PlayerPrefs; a counter has nothing to re-read from and has to say so out loud.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        live.Clear();
        trackedScene = null;
        lastRegisterFrame = int.MinValue;
        authored = 0;
        cleaned = 0;
    }

    // --- registration -------------------------------------------------------------------

    /// <summary>Called from <see cref="Litter.Awake"/>, before any Litter has deleted itself.</summary>
    public static void Register(Litter piece)
    {
        NewLoadCheck();
        lastRegisterFrame = Time.frameCount;
        authored++;
        if (piece != null && !live.Contains(piece)) live.Add(piece);
    }

    public static void Unregister(Litter piece)
    {
        if (piece != null) live.Remove(piece);
    }

    /// <summary>
    /// A piece that was already cleaned on an earlier visit and is deleting itself: keep it in
    /// the count and repaint its ground, but don't re-award the percentage the codex already
    /// banked, and don't sparkle for something the player did last time.
    /// </summary>
    public static void RestoreCleaned(Vector3 at)
    {
        SyncScene();
        cleaned++;
        TintGround(at, PieceRadius);
    }

    // --- cleaning -----------------------------------------------------------------------

    /// <summary>
    /// One piece of trash cleared: green the ground under it, sparkle, and move the stage's
    /// meter to wherever the running count says it should now be.
    /// </summary>
    public static void Clean(Vector3 at)
    {
        SyncScene();
        cleaned++;
        TintGround(at, PieceRadius);
        Vfx.CleanBurst(at, PieceRadius * 0.7f);

        int stage = StageId();
        if (stage == 0 || authored <= 0) return;

        float target = 100f * cleaned / authored;
        Codex.AddCleanliness(stage, target - Codex.GetCleanliness(stage));
    }

    /// <summary>
    /// Clear every piece of trash inside a radius — the Seed Bomb's other half (§4.7.2: an AoE
    /// that <i>clears trash</i> as well as damaging). Returns how many pieces it caught.
    /// </summary>
    public static int CleanRadius(Vector3 center, float radius)
    {
        float sqr = radius * radius;
        int n = 0;
        // Copied first: Litter.Clean destroys the piece, which unregisters it mid-walk.
        var batch = new List<Litter>(live);
        foreach (var piece in batch)
        {
            if (piece == null) continue;
            Vector3 d = piece.transform.position - center;
            d.y = 0f;                                   // reach is XZ, like everything else here
            if (d.sqrMagnitude > sqr) continue;
            piece.Clean();
            n++;
        }
        return n;
    }

    // --- ground -------------------------------------------------------------------------

    // Repaints the ground the way ReclamationPatch does, and for the same reason: an opaque
    // URP mesh has no alpha to cross-fade, so "this is clean now" has to be carried by colour.
    //
    // The footprint cap is load-bearing, not a tweak. Tinting works per renderer, and the two
    // levels build their floors completely differently: Level 1 lays 192 four-metre tiles, so
    // a cleansed piece greens the tile it sits on and maybe a neighbour, while Level 2's floor
    // is a *single* 40 × 34 m slab that export_level2.py merged out of 1 360 cells. Without the
    // cap, one bottle in the factory would repaint the entire level in one frame.
    static void TintGround(Vector3 at, float radius)
    {
        var clean = StageId() == Codex.StageFactory ? FactoryClean : ValleyClean;
        float maxArea = radius * radius * 12f;
        int ground = LayerMask.NameToLayer("Ground");
        var hits = Physics.OverlapSphere(at, radius, ~0, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (h.gameObject.layer != ground) continue;
            if (!h.TryGetComponent<Renderer>(out var r)) continue;
            var size = r.bounds.size;
            if (size.x * size.z > maxArea) continue;   // a slab far bigger than the patch
            MaterialTint.Apply(r, clean);
        }
    }

    // --- bookkeeping --------------------------------------------------------------------

    /// <summary>
    /// Start a fresh tally when this registration belongs to a different load of a level.
    ///
    /// <para>The obvious test — "has the active scene's name changed?" — is not enough, and the
    /// case it misses is one the player hits routinely: <b>reloading the level you are already
    /// in</b>. Dying and taking "Chơi lại" reloads the same scene, so the name never changes,
    /// and the tally simply carried on counting: sixteen authored pieces in an eight-piece
    /// field, every piece worth half of what it should be, and 100% permanently out of reach.
    /// Both level scenes are loaded whole, so every Litter in them registers inside the same
    /// <c>Awake</c> burst — a registration arriving a frame or more after the last one can only
    /// belong to a new load. (Nothing in the game spawns litter mid-level; anything that did
    /// would be read as the start of a new stage, which is the assumption to revisit first if
    /// that ever changes.)</para>
    /// </summary>
    static void NewLoadCheck()
    {
        string now = SceneManager.GetActiveScene().name;
        if (now == trackedScene && Time.frameCount <= lastRegisterFrame + 1) return;
        trackedScene = now;
        live.Clear();
        authored = 0;
        cleaned = 0;
    }

    // The read paths only need the scene name to be current; they must never reset the tally.
    static void SyncScene()
    {
        string now = SceneManager.GetActiveScene().name;
        if (now != trackedScene) NewLoadCheck();
    }

    static int StageId() => trackedScene switch
    {
        "Level1_BarrenFarm" => Codex.StageValley,
        "Level2_FactoryMaze" => Codex.StageFactory,
        _ => 0,                       // the hub and the story scenes have no meter
    };
}
