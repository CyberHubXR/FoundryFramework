using System;
using System.Collections.Generic;
using System.IO;
using Foundry.Services;
using UnityEngine;

namespace Foundry
{
    /// <summary>
    /// Contains all settings to do with the foundry core package, not to be confused with the foundry app config, which stores settings about the application as a whole.
    /// </summary>
    public class FoundryCoreConfig : FoundryModuleConfig
    {
#if UNITY_EDITOR
        public static FoundryCoreConfig GetAsset()
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<FoundryCoreConfig>(
                "Assets/Foundry/Settings/FoundryCoreConfig.asset");  
            if (asset == null)
            {
                asset = CreateInstance<FoundryCoreConfig>();
                Directory.CreateDirectory("Assets/Foundry/Settings");
                UnityEditor.AssetDatabase.CreateAsset(asset, "Assets/Foundry/Settings/FoundryCoreConfig.asset");
            }
            return asset;
        }
#endif
        public override void RegisterServices(Dictionary<Type, ServiceConstructor> constructors)
        {
            constructors.Add(typeof(ISceneNavigator), () => new SceneNavigator());
            constructors.Add(typeof(IPlayerRigManager), () => new PlayerRigManagerService());
            constructors.Add(typeof(IFoundryCameraManager), () => new FoundryCameraManager());
        }
    }
}
