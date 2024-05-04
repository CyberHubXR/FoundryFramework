using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using CyberHub.Brane;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using System.Net;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif 

namespace Foundry.Networking
{
    public enum DisconnectBehaviour
    {
        /// <summary>
        /// Transfer ownership to the graph authority when the owner disconnects
        /// </summary>
        TransferOwnership,
        /// <summary>
        /// Destroy the object when the owner disconnects
        /// </summary>
        Destroy,
    }
    
    /// <summary>
    /// Interface for foundry network objects to interact with the underlying network system.
    /// </summary>
    public interface INetworkObjectAPI
    {
        /// <summary>
        /// Get the network ID of this object. This will be invalid if the object is not in the graph.
        /// Setting this will result in an error, as the network ID is managed by the network system and can only be set once.
        /// </summary>
        public NetworkId NetworkStateId { get; set; }
        
        /// <summary>
        /// Get the player that owns this object. In rare cases (usually errors) this may not match the owner as represented by the foundry network state.
        /// </summary>
        public int Owner { get; }
        
        /// <summary>
        /// Return true if the native network object is owned by the local player.
        /// </summary>
        public bool IsOwner { get; }

        /// <summary>
        /// Get the network state id paired to this object once it is created. The callback will not be called until id is valid.
        /// </summary>
        /// <param name="callback">Action to preform once Id is set to a valid value. Will only be called once.</param>
        public void GetNetworkStateIdAsync(Action<NetworkId> callback);
        
        /// <summary>
        /// Callback for when the native network object is connected to the network.
        /// </summary>
        /// <param name="callback">Action to be performed when a connection is established</param>
        public void OnConnected(Action callback);
        
        /// <summary>
        /// Set a callback to verify if a requested ownership change should be allowed. This will be called on only the client that currently owns the object.
        /// </summary>
        /// <param name="callback">Callback to perform validation, return true to allow the change</param>
        public void OnValidateOwnershipChange(Func<int, bool> callback);

        /// <summary>
        /// Set a callback to perform an action when the owner of this object changes. This will be called on all clients.
        /// </summary>
        /// <param name="callback">Action to be performed, the new owner of the object is passed in</param>
        public void OnOwnershipChanged(Action<int> callback);
        
        /// <summary>
        /// Set the ownership of this object. This will only work if the object is owned by the local player, or if the object has transfer ownership enabled and the old owner allows it.
        /// Until the callback is called, the object will still be owned by the old owner, but if setting ownership to the local player the object will be treated locally as owned by the local player while not sending updates until the callback is called.
        /// </summary>
        /// <param name="newOwner">The player to transfer ownership to</param>
        /// <param name="callback">Callback to preform once an ownership change has succeeded or failed, with the passed bool representing the result</param>
        public void SetOwnership(int newOwner, Action<bool> callback);
        
        /// <summary>
        /// If we missed a delta containing the initial state of this object, request it from the owner.
        /// </summary>
        /// <param name="callback">Callback is performed once we've received the node and it has been added to the state graph</param>
        public void RequestFullState(Action callback);
    }
    
    public class NetworkObject : MonoBehaviour
    {
        [HideInInspector]
        public MonoBehaviour nativeScript;

        [Tooltip("What should happen if the owner of this object disconnects?")]
        public DisconnectBehaviour disconnectBehaviour;
        
        /// <summary>
        /// Allow other clients to take control of this object
        /// </summary>
        public bool allowOwnershipTransfer = true;
        
        /// <summary>
        /// When the ownership of this object changes, this event is invoked with a reference to this object and the new owner.
        /// </summary>
        public UnityEvent<NetworkObject, int> OnOwnerChanged;

        /// <summary>
        /// API component for interacting with the underlying network system.
        /// </summary>
        private INetworkObjectAPI api
        {
            get
            {
                if (_api != null)
                    return _api;
                _api = GetComponent<INetworkObjectAPI>();
                return _api;
            }
        }

        private INetworkObjectAPI _api;

        /// <summary>
        /// Returns the NetworkGraphId script attached to this object, or null if it doesn't exist.
        /// </summary>
        public NetworkId Id => api.NetworkStateId;
        
        /// <summary>
        /// The player that owns this object. This will be -1 if the object is not in the graph.
        /// </summary>
        public int Owner => api.Owner;

        /// <summary>
        /// Returns the current network provider instance.
        /// </summary>
        public INetworkProvider NetworkProvider
        {
            get
            {
                if(networkProvider != null)
                    return networkProvider;
                networkProvider = BraneApp.GetService<INetworkProvider>();
                return networkProvider;
            }
        }
        private INetworkProvider networkProvider;

        /// <summary>
        /// Returns if this object is owned by the local player. This will return true if there is no network session or OnConnected() has not been called yet.
        /// </summary>
        public bool IsOwner
        {
            get
            {
                // This object has not been baked, so there is likely no network session running.
                if (api == null)
                    return true;

                if (NetworkManager.instance == null)
                    return true;
                
                // Are we the owner of this object?
                return Owner == NetworkProvider.LocalPlayerId;
            }
        }

        /// <summary>
        /// Callback for validating if an ownership change should be allowed. This will be called on only the client that currently owns the object.
        /// </summary>
        private Func<int, bool> ValidateOwnershipChange;

        /// <summary>
        /// The node this object is linked too. This is null if the object is not in the graph.
        /// </summary>
        private NetworkObjectState associatedNode
        {
            set => _associatedNode = value;
            get
            {
                if (_associatedNode)
                    return _associatedNode;
                if (Id.IsValid())
                    UpdateBoundNode();
                return _associatedNode;
            }
        }
        private NetworkObjectState _associatedNode;
        
        /// <summary>
        /// All the networked components owned by this object.
        /// </summary>
        [HideInInspector]
        public List<NetworkComponent> NetworkComponents = new();
        
        private List<INetworkProperty> networkProperties;
        public List<INetworkProperty> NetworkProperties
        {
            get
            {
                if (networkProperties == null)
                    BuildProperties();
                return networkProperties;
            }
        }

        /// <summary>
        /// GUID for this object. This is used to identify objects across sessions. Many networking systems require this.
        /// </summary>
        [HideInInspector]
        public string guid;
        
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            bool shouldChangeGUI = string.IsNullOrEmpty(guid);
            try
            {
                if (PrefabUtility.GetPrefabInstanceStatus(this) == PrefabInstanceStatus.Connected)
                {
                    var og = PrefabUtility.GetCorrespondingObjectFromSource(this);
                
                    var stage = PrefabStageUtility.GetPrefabStage(this.gameObject);
                    bool isPrefab = stage?.IsPartOfPrefabContents(this.gameObject) ?? false;
                
                    // If this is a scene instance make sure we have a unique guid
                    if(!isPrefab)
                        shouldChangeGUI |= og.guid == guid;
                }

                foreach (var so in Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
                {
                    if(so == this)
                        continue;
                    shouldChangeGUI |= so.guid == guid;
                }
            } catch (Exception e)
            {
                // Failed to get prefab status, probably because this was called from Awake or OnEnable.
            }
            
            if (shouldChangeGUI)
            {
                guid = Guid.NewGuid().ToString();
                EditorUtility.SetDirty(this);
            }
            
            UpdateComponents();
        }
        
        internal void UpdateComponents()
        {
            UpdateComponentsRecursive(transform);
        }
        
        private void UpdateComponentsRecursive(Transform t)
        {
            var oldComponents = NetworkComponents;
            if (t == transform)
                NetworkComponents = new();
            if (t.gameObject.TryGetComponent(out NetworkObject otherObj))
            {
                if (otherObj != this)
                    return;
            }
            
            var networkComponents = t.GetComponents<NetworkComponent>();
            NetworkComponents.AddRange(networkComponents);
            
            foreach (Transform child in t)
                UpdateComponentsRecursive(child);

            if (t == transform)
            {
                bool componentsChanged = oldComponents.Count != NetworkComponents.Count;
                if (!componentsChanged)
                {
                    for (int i = 0; i < oldComponents.Count; i++)
                    {
                        if (oldComponents[i] != NetworkComponents[i])
                        {
                            componentsChanged = true;
                            break;
                        }
                    }
                }
                
                if(componentsChanged)
                    EditorUtility.SetDirty(this);
            }
        }
#endif
        void Awake()
        {
            BuildProperties();
        }

        //Load steps
        private bool nodeAssigned;
        private bool idAssigned;
        private int completedLoadSteps = 0;
        private readonly int totalLoadSteps = 2;
        private bool registeredUnloaded;
        
        void Start()
        {
            if (NetworkManager.instance)
            {
                NetworkManager.RegisterUnloaded(this);
                registeredUnloaded = true;
                
                api.OnConnected(()=> {
                    api.GetNetworkStateIdAsync(async id =>
                    {
                        UpdateBoundNode();
                        NetworkManager.RegisterObject(this);
                        CompleteLoadStep(ref idAssigned);
                        
                        // Hail Mary last ditch effort to get the node if we missed it.
                        while (!associatedNode)
                        {
                            await Task.Delay(500);
                            if (!associatedNode)
                            {
                                api.RequestFullState(() =>
                                {
                                    Debug.Log("Obtained full node for " + gameObject.name + " manually");
                                    UpdateBoundNode();
                                });
                            }
                        } 
                    });
                });
                
                api.OnValidateOwnershipChange(VerifyIDChangeRequest);
                
                api.OnOwnershipChanged(newOwner =>
                {
                    // If we detect that we are the old owner and the new owner is not us, we should change the owner.
                    if (IsOwner && newOwner != NetworkProvider.LocalPlayerId)
                        NetworkManager.State.ChangeOwner(Id, newOwner);
                    OnOwnerChanged.Invoke(this, newOwner);
                });
            }
        }

        internal void OnSceneReady()
        {
            foreach (var networkedComponent in NetworkComponents)
            {
                try
                {
                    networkedComponent.OnConnected();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        void CompleteLoadStep(ref bool loadStep)
        {

            // If we've already completed this step, don't do it again.
            if (loadStep)
                return;
            loadStep = true;
            
            // If we haven't completed all the steps, don't do anything else.
            if(++completedLoadSteps < totalLoadSteps)
                return;

            NetworkManager.RegisterLoaded(this);
            registeredUnloaded = false;
        }

        void OnDestroy()
        {
            if (NetworkManager.instance)
            {
                if(Id.IsValid())
                    NetworkManager.UnregisterObject(this);
                if(registeredUnloaded)
                    NetworkManager.RegisterLoaded(this);
            }
        }

        /// <summary>
        /// Builds the list of networked properties on this object.
        /// </summary>
        internal void BuildProperties()
        {
            if (networkProperties != null)
                return;
            
            networkProperties = new();
            
            foreach (var networkedComponent in NetworkComponents)
            {
                networkedComponent.Object = this;
                networkedComponent.RegisterProperties(networkProperties);
            }
        }

        /// <summary>
        /// Tells this object to create a network state for itself.
        /// </summary>
        /// <param name="state"></param>
        internal void CreateState()
        {
            NetworkObjectState newNode;
            if (!Id.IsValid())
            {
                newNode = NetworkManager.State.CreateNode();
                api.NetworkStateId = newNode.Id;
            }
            else
                newNode = NetworkManager.State.AddNode(Id);
            
            LinkState(newNode);
        }

        /// <summary>
        /// Attempts to connect to the graph node associated with this object using the NetworkedGraphId and register properties. Does nothing if the node is already connected.
        /// </summary>
        internal void UpdateBoundNode()
        {
            // If we have a node already, we don't need to do anything.
            if(_associatedNode)
                return;
            
            if (NetworkManager.State.TryGetNode(Id, out NetworkObjectState node))
               LinkState(node);
            else if (IsOwner && Id.IsValid())
                CreateState();
        }

        /// <summary>
        /// Set up a new node with the correct data
        /// </summary>
        /// <param name="node"></param>
        private void LinkState(NetworkObjectState node)
        {
            Debug.Assert(node);
            node.AssociatedObject = this;
            associatedNode = node;
            node.allowOwnershipTransfer = allowOwnershipTransfer;
            Debug.Assert(node.Id == Id);

            node.Properties = NetworkProperties;
            
            // If there's un-serialized data for this node, consume that now that our properties have been initalized.
            node.ConsumeCachedProps();

            CompleteLoadStep(ref nodeAssigned);
        }
        
        /// <summary>
        /// Set a callback to verify if a network ID change from a client without ownership should be allowed. This will
        /// be called on every remote client, not just the owner so make sure that it's consistent between clients.
        /// </summary>
        /// <param name="callback">Callback that performs the verification, it's passed an int representing the new requested owner, and should return a bool representing if the change should be allowed to continue</param>
        public void SetOwnershipVerificationCallback(Func<int, bool> callback)
        {
            ValidateOwnershipChange = callback;
        }
        
        /// <summary>
        /// Returns if an ownership change should be allowed. This is executed locally and is intended for internal use.
        /// </summary>
        /// <param name="newOwner"></param>
        /// <param name="newId"></param>
        /// <returns></returns>
        public bool VerifyIDChangeRequest(int newOwner)
        {
            if (ValidateOwnershipChange == null)
                return allowOwnershipTransfer;
            return ValidateOwnershipChange(newOwner);
        }
        
        /// <summary>
        /// Request ownership of an object with transfer ownership enabled. 
        /// </summary>
        public void RequestOwnership()
        {
            
            if (!allowOwnershipTransfer)
            {
                Debug.LogError($"Attempted to take ownership of {gameObject.name} but allowOwnershipTransfer is false.");
                return;
            }

            if (!associatedNode)
            {
                Debug.LogError("Attempted to take ownership of an object that is not in the graph.");
                return;
            }
            
            if (associatedNode.owner == NetworkProvider.LocalPlayerId && api.IsOwner)
                return;
            
            api.SetOwnership(NetworkProvider.LocalPlayerId, (success) =>
            {
                if (!success)
                {
                    Debug.LogError($"Failed to take ownership of {gameObject.name}.");
                    return;
                }
                Debug.Log($"Took ownership of {gameObject.name}.");
                NetworkManager.State.ChangeOwner(Id, NetworkProvider.LocalPlayerId);
            });
        }
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(NetworkObject))]
    public class NetworkObjectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var networkObject = (NetworkObject)target;
            
            EditorGUI.BeginDisabledGroup(true);
            
            
            EditorGUILayout.LabelField("Network Components:");
            foreach (var property in networkObject.NetworkComponents)
                EditorGUILayout.ObjectField(property, typeof(NetworkComponent), true);
            if(networkObject.NetworkComponents.Count == 0)
                EditorGUILayout.LabelField("None");
            EditorGUILayout.Space();
            
            EditorGUI.indentLevel = 0;
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.TextField("Guid", networkObject.guid);
            
            if (Application.isPlaying && NetworkManager.instance != null)
            {
                EditorGUILayout.LabelField("Is Owner: " + networkObject.IsOwner);
                EditorGUILayout.LabelField("Network ID: " + (networkObject.Id.ToString() ?? "Not set"));
                EditorGUILayout.LabelField("Owner: " + (networkObject.Owner == -1 ? "None" : networkObject.Owner.ToString()));
                EditorGUILayout.LabelField("Networked Properties: " + networkObject.NetworkComponents.Count);
                
                EditorGUI.BeginDisabledGroup(!networkObject.allowOwnershipTransfer);
                if (GUILayout.Button("Request Ownership"))
                    networkObject.RequestOwnership();
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                var stage = PrefabStageUtility.GetPrefabStage(networkObject.gameObject);
                bool isPrefab = stage?.IsPartOfPrefabContents(networkObject.gameObject) ?? false;
                if (isPrefab)
                {
                    // Get the actual prefab object
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(stage.assetPath);
                    if (!FoundryPrefabs.IsInPrefabList(prefab))
                    {
                        GUIStyle style = new GUIStyle();
                        style.normal.textColor = Color.yellow;
                        EditorGUILayout.LabelField("This prefab is not in Foundry prefab list! NetworkManager.Instantiate will not work for this object.", style);
                        if (GUILayout.Button("Add to prefab list"))
                            FoundryPrefabs.AddPrefab(prefab);
                    }
                }
                
            }
            
        }
    }
#endif 
}