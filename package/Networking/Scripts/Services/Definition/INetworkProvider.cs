using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Foundry.Services;
using UnityEngine;

namespace Foundry.Networking
{
    /// <summary>
    /// Delegate for events handling network events with no arguments
    /// </summary>
    public delegate void NetworkEventHandler();

    /// <summary>
    /// Delegate for handling network events to do with errors
    /// TODO - Add error codes or enum for easier case-by-case handling
    /// </summary>
    public delegate void NetworkErrorEventHandler(string error);
    
    /// <summary>
    /// Delegate for handling network events to do with players
    /// </summary>
    public delegate void NetworkPlayerEventHandler(int player);

    /// <summary>
    /// Interface for a networking provider
    /// </summary>
    public interface INetworkProvider
    {
        /// <summary>
        /// Starts a network session asynchronously
        /// </summary>
        /// <param name="info">Information about the session to start.</param>
        /// <remarks>
        /// This method should create whatever context a networking system requires to create and maintain a session.
        /// This session should not be tied to the lifetime of a scene, so any game objects created must be marked as
        /// do no destroy on load.
        /// </remarks>
        Task StartSessionAsync(SessionInfo info);
        
        
        /// <summary>
        /// Stops the current network session asynchronously
        /// </summary>
        /// <returns>Returns a task handle</returns>
        Task StopSessionAsync();

        /// <summary>
        /// Link foundry network components to native network components and complete any other necessary setup.
        /// All child objects of the provided game object will be bound as well.
        /// </summary>
        /// <param name="gameObject">Object to be bound</param>
        /// <param name="isPrefab">Whether the object is a prefab or not</param>
        void BindNetworkObject(GameObject gameObject, bool isPrefab);
        
        /// <summary>
        /// Register a prefab to be used for later Instantiate calls. Prefabs must have had BindNetworkObject called on
        /// them beforehand.
        /// </summary>
        /// <param name="prefab">Prefab to be registered</param>
        void RegisterPrefab(GameObject prefab);
    
        /// <summary>
        /// Tell a network provider that we're done loading and setting up a scene, so it may now do whatever it needs to with it.
        /// </summary>
        /// <param name="scene">Index and name of scene that was loaded.</param>
        Task CompleteSceneSetup(ISceneNavigationEntry scene);
    
        /// <summary>
        /// Spawn a networked object instance over the network.
        /// </summary>
        /// <param name="prefab">Prefab to be spawned. Must be registered with this interface or the native provider.</param>
        /// <param name="position">Position to spawn at</param>
        /// <param name="rotation">Rotation to spawn with</param>
        /// <returns>Returns the instantiated game object</returns>
        GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation);
        
        /// <summary>
        /// Destroy a networked object over the network.
        /// </summary>
        /// <param name="gameObject">Object to be destroyed</param>
        void Destroy(GameObject gameObject);
        
        
        SessionType sessionType { get; }
        
        bool IsSessionConnected { get;  }
        
        /// <summary>
        /// Returns true if this instance is hosting a server
        /// </summary>
        bool IsServer { get; }
        
        /// <summary>
        /// Returns true if this instance is running as a client
        /// </summary>
        bool IsClient { get; }
        
        /// <summary>
        /// Returns true if this instance is the authority for the scene graph
        /// </summary>
        bool IsGraphAuthority { get; }
        
        /// <summary>
        /// Returns the ID of the local player
        /// </summary>
        int LocalPlayerId { get; }
        
        /// <summary>
        /// Obtain the current network graph, may be null if no session is active
        /// </summary>
        public NetworkState State { get; }

        /// <summary>
        /// Raised when a session has connected
        /// </summary>
        event NetworkEventHandler SessionConnected;
        
        /// <summary>
        /// Raised when a session has failed to start
        /// </summary>
        event NetworkErrorEventHandler StartSessionFailed;
        
        /// <summary>
        /// Raised when a session has disconnected.
        /// </summary>
        event NetworkErrorEventHandler SessionDisconnected;

        /// <summary>
        /// Raised when a player has joined the session
        /// </summary>
        event NetworkPlayerEventHandler PlayerJoined;
        
        /// <summary>
        /// Raised when a player has left the session
        /// </summary>
        event NetworkPlayerEventHandler PlayerLeft;
    }
}
