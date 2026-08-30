using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// "Does a screen own the player right now?" — one question, one answer, for the systems that
/// have to stop when a panel is up.
///
/// <para>B6 needed this because the perspective toggle has two obligations no existing check
/// covered: <b>P</b> must not fire while a modal is open, and the mouse cursor must be released
/// the moment one opens or the player cannot click a consumable in the bag. Most of the game's
/// modals already announce themselves through the clock — <see cref="PauseController"/>,
/// <c>DialogueRunner</c>, <see cref="TutorialPopup"/> and the end screens all park
/// <c>Time.timeScale</c> at exactly 0, and <see cref="GameFeel"/>'s hit-stop deliberately
/// crawls at 0.02 instead so it never reads as one. The four runtime-built panels (bag, codex,
/// quest log, crafting) and the shop do <i>not</i> touch the clock, so they register here.</para>
///
/// <para>Owners are tracked by instance id rather than by a counter: a counter is the classic
/// Fast-Enter-Play-Mode leak (CLAUDE.md rule 4) — one panel left open when play mode exits and
/// the count never comes back to zero. A set of ids is idempotent, so a double
/// <c>Set(this, true)</c> costs nothing, and it is cleared on every play session anyway.</para>
/// </summary>
public static class UiModal
{
    static readonly HashSet<int> open = new HashSet<int>();

    /// <summary>True while any screen has the player's attention.</summary>
    public static bool AnyOpen =>
        open.Count > 0
        || DialogueRunner.IsActive
        || TutorialPopup.IsOpen
        || Mathf.Approximately(Time.timeScale, 0f);

    /// <summary>Register or clear one screen. Safe to call every time it opens or closes.</summary>
    public static void Set(Object owner, bool isOpen)
    {
        if (owner == null) return;
        if (isOpen) open.Add(owner.GetInstanceID());
        else open.Remove(owner.GetInstanceID());
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => open.Clear();
}
