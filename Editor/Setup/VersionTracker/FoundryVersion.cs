using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "FoundryInstalledVersion", menuName = "Foundry/FoundryVersionTracker", order = 2)]
public class FoundryVersion : ScriptableObject
{
    public string version;

    private void OnValidate()
    {
        EditorUtility.SetDirty(this);
    }
}
