using UnityEditor;
using UnityEngine;

public static class RebuildLevel1Now
{
    [MenuItem("Eco-Dash/Rebuild Level 1 Now")]
    public static void Run()
    {
        Debug.Log("Executing Level1Builder.Execute()...");
        string result = Level1Builder.Execute();
        Debug.Log("Rebuild Level 1 Result: " + result);
    }
}
