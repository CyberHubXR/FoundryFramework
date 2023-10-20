using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace Foundry.Networking
{
    /// <summary>
    /// Cache of all prefabs we need to bake for Foundry networking when an app starts.
    /// </summary>
    public class FoundryPrefabs : ScriptableObject
    {
        [Tooltip("Prefabs that should be Baked for Foundry networking")]
        public List<GameObject> networkedPrefabs = new();
        
    #if UNITY_EDITOR
        public static FoundryPrefabs GetAsset()
        {
            var asset = Resources.Load<FoundryPrefabs>("FoundryPrefabs");
            if (asset == null)
            {
                asset = CreateInstance<FoundryPrefabs>();
                Directory.CreateDirectory("Assets/Foundry/Resources");
                AssetDatabase.CreateAsset(asset, "Assets/Foundry/Resources/FoundryPrefabs.asset");
            }

            return asset;
        }
        
        [MenuItem("Foundry/Manage Prefabs")]
        public static void OpenPrefabsAsset()
        {
            var asset = GetAsset();
            Selection.activeObject = asset;
        }
        
        public static bool IsInPrefabList(GameObject prefab)
        {
            var asset = GetAsset();
            return asset.networkedPrefabs.Contains(prefab);
        }

        public static void AddPrefab(GameObject prefab)
        {
            if (!prefab.TryGetComponent(out NetworkObject obj))
            {
                Debug.LogError("Cannot add prefab to FoundryPrefabs: prefab must have a NetworkObject component on it's root gameObject");
                return;
            }
            var asset = GetAsset();
            if (asset.networkedPrefabs.Contains(prefab))
            {
                Debug.LogWarning("Prefab is already in FoundryPrefabs");
                return;
            }

            asset.networkedPrefabs.Add(prefab);
            EditorUtility.SetDirty(asset);
        }
    #endif
    }
}
