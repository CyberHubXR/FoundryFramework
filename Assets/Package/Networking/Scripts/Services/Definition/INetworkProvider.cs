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
        /// Delegate for handling network events to do with state changes, with the sender and and serialized sent delta
        /// </summary>
        delegate void StateDeltaCallback(int sender, byte[] delta);
        
        /// <summary>
        /// Notify all other clients that you would like to start receiving state deltas from them.
        /// They will then send you their current state in a full delta and any future changes.
        ///
        /// When new clients join, you will also subscribe to their state deltas.
        /// </summary>
        /// <param name="onStateDelta">Action to perform every time a delta is received</param>
        /// <returns>Task that completes once you have received an initial delta from every other client on the network, this is so we can pause logic that requires initial data until that point</returns>
        Task SubscribeToStateChangesAsync(StateDeltaCallback onStateDelta);

        /// <summary>
        /// Set a callback for grabbing an initial state to send to players when they subscribe to changes from this client.
        /// </summary>
        /// <param name="callback">This callback expects a byte array to be returned, with the full local network state</param>
        void SetSubscriberInitialStateCallback(Func<byte[]> callback);
        
        /// <summary>
        /// Send a delta of our local state to all other clients.
        /// </summary>
        /// <param name="delta">serialized delta data</param>
        void SendStateDelta(byte[] delta);
    
        /// <summary>
        /// Spawn a networked object instance over the network.
        /// </summary>
        /// <param name="prefab">Prefab to be spawned. Must be registered with this interface or the native provider.</param>
        /// <param name="position">Position to spawn at</param>
        /// <param name="rotation">Rotation to spawn with</param>
        /// <returns>Returns the instantiated game object</returns>
        GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation);
        
        /// <summary>
        /// Destroy a networked object over the network.
        /// </summary>
        /// <param name="gameObject">Object to be destroyed</param>
        void Despawn(GameObject gameObject);
        
        
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
        /// Get the ID of the player or server that has authority over the scene graph
        /// </summary>
        int GraphAuthorityId { get; }
        
        /// <summary>
        /// Returns the ID of the local player
        /// </summary>
        int LocalPlayerId { get; }

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
