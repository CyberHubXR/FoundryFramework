using System;
using System.Collections.Generic;
using CyberHub.Foundry;
using Foundry.Services;

namespace Foundry
{
    /// <summary>
    /// Contains all settings to do with the foundry core package, not to be confused with the foundry app config, which stores settings about the application as a whole.
    /// </summary>
    public class FoundryFrameworkConfig : FoundryModuleConfig
    {
#if UNITY_EDITOR
        public static FoundryFrameworkConfig GetAsset()
        {
            return GetOrCreateAsset<FoundryFrameworkConfig>("FoundryFrameworkConfig.asset"); 
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
