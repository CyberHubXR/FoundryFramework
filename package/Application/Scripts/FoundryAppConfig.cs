using System;
using System.IO;
using UnityEngine;

namespace Foundry
{
    public class FoundryAppConfig : ScriptableObject
    {
        public FoundryModuleConfig[] modules = Array.Empty<FoundryModuleConfig>();
        public void RegisterServices(FoundryApp app)
        {
            foreach (var module in modules)
                module.RegisterServices(app);
        }
        
#if UNITY_EDITOR
        public static FoundryAppConfig GetAsset()
        {
            var asset = Resources.Load<FoundryAppConfig>("FoundryAppConfig");
            if (asset == null)
            {
                asset = CreateInstance<FoundryAppConfig>();
                Directory.CreateDirectory("Assets/Foundry/Resources");
                UnityEditor.AssetDatabase.CreateAsset(asset, "Assets/Foundry/Resources/FoundryAppConfig.asset");
            }

            return asset;
        }
#endif
    }
}
