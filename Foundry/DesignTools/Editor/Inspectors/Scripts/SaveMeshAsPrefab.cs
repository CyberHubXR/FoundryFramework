#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class SaveMeshAsPrefab : MonoBehaviour {
    public SkinnedMeshRenderer skinnedMeshRenderer;

    [ContextMenu("SAVE")]
    public void Save() {
        SaveMesh(skinnedMeshRenderer.sharedMesh, "POSER_MESH");
    }

    public void SaveMesh(Mesh mesh, string assetName) {
        // Ensure the Resources folder exists
        if(!AssetDatabase.IsValidFolder("Assets/Resources")) {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        // Save the Mesh as an asset in the Resources folder
        AssetDatabase.CreateAsset(mesh, "Assets/Resources/" + assetName + ".asset");

        // Save and refresh the assets database
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Load the saved mesh
        Mesh loadedMesh = Resources.Load<Mesh>(assetName);

        // Apply the loaded mesh to the SkinnedMeshRenderer
        skinnedMeshRenderer.sharedMesh = loadedMesh;
    }
}
#endif
