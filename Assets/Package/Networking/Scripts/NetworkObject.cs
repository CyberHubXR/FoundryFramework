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
    public enum DisconnectBehaviour: byte
    {
        /// <summary>
        /// Transfer ownership to the graph authority when the owner disconnects
        /// </summary>
        TransferOwnership = 0,
        /// <summary>
        /// Destroy the object when the owner disconnects
        /// </summary>
        Destroy = 1,
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
        /// Returns the NetworkGraphId script attached to this object, or null if it doesn't exist.
        /// </summary>
        public NetworkId Id => state?.Id ?? NetworkId.Invalid;
        
        /// <summary>
        /// The player that owns this object. This will be -1 if the object is not in the graph.
        /// </summary>
        public UInt64 Owner => state?.owner ?? UInt64.MaxValue;

        /// <summary>
        /// Returns if this object is owned by the local player. This will return true if there is no network session or OnConnected() has not been called yet.
        /// </summary>
        public bool IsOwner
        {
            get
            {
                var netMan = NetworkManager.instance;
                if (netMan == null || !netMan.IsSessionConnected)
                    return true;
                
                // Are we the owner of this object?
                return Owner == netMan.LocalPlayerId;
            }
        }

        /// <summary>
        /// Callback for validating if an ownership change should be allowed. This will be called on only the client that currently owns the object.
        /// </summary>
        private Func<UInt32, bool> ValidateOwnershipChange;

        /// <summary>
        /// The node this object is linked too. This is null if the object is not in the graph.
        /// </summary>
        private NetworkEntity state;
        
        /// <summary>
        /// All the networked components owned by this object.
        /// </summary>
        [HideInInspector]
        public List<NetworkComponent> NetworkComponents = new();
        
        private List<INetworkProperty> networkProperties;
        private List<INetworkEvent> networkEvents;

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
        
        void Start()
        {
            if (NetworkManager.instance)
            {
                
                /*api.OnConnected(()=> {
                    api.GetNetworkStateIdAsync(async id =>
                    {
                        UpdateBoundNode();
                        NetworkManager.RegisterObject(this);
                        CompleteLoadStep(ref idAssigned);
                        
                        // Hail Mary last ditch effort to get the node if we missed it.
                        while (!State)
                        {
                            await Task.Delay(500);
                            if (!State)
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
                });*/
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
            networkEvents = new();
            
            foreach (var networkedComponent in NetworkComponents)
            {
                networkedComponent.Object = this;
                networkedComponent.RegisterProperties(networkProperties, networkEvents);
            }
        }

        /// <summary>
        /// Set up a new entity with the correct data
        /// </summary>
        /// <param name="node"></param>
        internal NetworkEntity CreateEntity()
        {
            NetworkEntity entity = new();
            entity.AssociatedObject = this;
            entity.objectId = guid;
            entity.disconnectBehaviour = disconnectBehaviour;
            state = entity;
            entity.allowOwnershipTransfer = allowOwnershipTransfer;

            entity.Properties = networkProperties;
            entity.Events = networkEvents;
            return entity;
        }
        
        /// <summary>
        /// Link this object to an existing entity
        /// </summary>
        /// <param name="node"></param>
        internal void LinkEntity(NetworkEntity node)
        {
            Debug.Assert(node.objectId == guid, "Guids did not match!");
            state = node;
            node.AssociatedObject = this;
            allowOwnershipTransfer = node.allowOwnershipTransfer;
            node.Properties = networkProperties;
            node.Events = networkEvents;
        }

        /// <summary>
        /// Called by the network manager once all of the object's properties have been registered and initialized.
        /// </summary>
        internal void InvokeConnected()
        {
            foreach (var networkComponent in NetworkComponents)
                networkComponent.OnConnected();
        }
        
        /// <summary>
        /// Set a callback to verify if a network ID change from a client without ownership should be allowed. This will
        /// be called on every remote client, not just the owner so make sure that it's consistent between clients.
        /// </summary>
        /// <param name="callback">Callback that performs the verification, it's passed an int representing the new requested owner, and should return a bool representing if the change should be allowed to continue</param>
        public void SetOwnershipVerificationCallback(Func<UInt32, bool> callback)
        {
            ValidateOwnershipChange = callback;
        }
        
        /// <summary>
        /// Returns if an ownership change should be allowed. This is executed locally and is intended for internal use.
        /// </summary>
        /// <param name="newOwner"></param>
        /// <param name="newId"></param>
        /// <returns></returns>
        public bool VerifyIDChangeRequest(UInt32 newOwner)
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

            if (!state)
            {
                Debug.LogError("Attempted to take ownership of an object that is not in the graph.");
                return;
            }
            
            throw new NotImplementedException();
            
            /*if (State.owner == NetworkProvider.LocalPlayerId && api.IsOwner)
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
            });*/
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
                EditorGUILayout.LabelField("Owner: " + (networkObject.Owner == UInt64.MaxValue ? "None" : networkObject.Owner.ToString()));
                EditorGUILayout.LabelField("Networked Properties: " + networkObject.NetworkComponents.Count);
                EditorGUILayout.LabelField("Networked Events: " + networkObject.NetworkComponents.Count);
                
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