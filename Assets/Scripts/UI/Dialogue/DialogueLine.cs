using UnityEngine;

/// <summary>
/// One line of NPC / story dialogue: who is speaking and what they say, with an
/// optional portrait. Plain serializable data handed to <see cref="DialogueRunner"/>;
/// not a MonoBehaviour. Authored inline on NPCs (a <c>DialogueLine[]</c>).
/// </summary>
[System.Serializable]
public class DialogueLine
{
    [Tooltip("Speaker name shown on the name plate, e.g. \"Bà Tư\". Blank hides the plate.")]
    public string speaker;

    [TextArea(2, 4)]
    [Tooltip("The spoken line (Vietnamese to match the brief).")]
    public string text;

    [Tooltip("Optional portrait sprite shown beside the line.")]
    public Sprite portrait;
}
