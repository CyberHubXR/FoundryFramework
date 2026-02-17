using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Foundry.Services
{
    /// <summary>
    /// Represents a single entry in scene navigation history.
    /// </summary>
    public interface ISceneNavigationEntry
    {
        /// <summary>
        /// Gets a name for the scene.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Gets the build index of the scene.
        /// </summary>
        /// <remarks>
        /// Note that this number is not always reliable. For example, if the scene was not 
        /// included in the build window, this number will be one higher than the last built scene 
        /// and if the scene was loaded from an asset bundle this number will be -1.
        /// </remarks>
        int BuildIndex { get; }
        
        /// <summary>
        /// Gets the asset reference to the scene, if this scene is an addressable asset.
        /// </summary>>
        public AssetReference SceneAsset { get; set; }
    }
}