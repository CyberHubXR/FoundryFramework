using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif 

namespace Foundry.Networking
{
    /// <summary>
    /// Interface for associating graph nodes with networked objects.
    /// This must be synced outside of the network graph, as it is required to construct it.
    /// </summary>
    public interface INetworkedGraphId
    {
        public NetworkId Value { get; set; }
        
        /// <summary>
        /// Called when the graph id is assigned. This may happen multiple times during the lifetime of the object.
        /// If the ID is already assigned, the callback will be called immediately once.
        /// </summary>
        /// <param name="callback"></param>
        public void OnIdAssigned(Action<NetworkId> callback);
    }
    
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
        /// When the ownership of this object changes, this event is invoked with a reference to this object and the new ID.
        /// </summary>
        public UnityEvent<NetworkObject, NetworkId> OnIDChanged;

        /// <summary>
        /// Returns the NetworkGraphId script attached to this object, or null if it doesn't exist.
        /// </summary>
        public INetworkedGraphId NetworkedGraphId
        {
            get
            {
                if(networkedGraphId != null)
                    return networkedGraphId;
                networkedGraphId = GetComponent<INetworkedGraphId>();
                return networkedGraphId;
            }
        }
        private INetworkedGraphId networkedGraphId;

        /// <summary>
        /// Returns the current network provider instance.
        /// </summary>
        public INetworkProvider NetworkProvider
        {
            get
            {
                if(networkProvider != null)
                    return networkProvider;
                networkProvider = FoundryApp.GetService<INetworkProvider>();
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
                if (NetworkedGraphId == null)
                    return true;
                // Are we the owner of this object?
                if (NetworkedGraphId.Value.Owner == NetworkProvider.LocalPlayerId)
                    return true;
                // If the network ID is just invalid we are technically the local owner
                return !NetworkedGraphId.Value.IsValid();

            }
        }
        
        /// <summary>
        /// Call signature for validating a network ID change request.
        /// </summary>
        public delegate bool IDChangeRequestCallback(int sender, NetworkId newId);

        private IDChangeRequestCallback IDChangeRequest;

        /// <summary>
        /// The node this object is linked too. This is null if the object is not in the graph.
        /// </summary>
        private NetworkGraphNode associatedNode;
        
        /// <summary>
        /// Network object parented to this object. This is used to build the graph.
        /// </summary>
        [HideInInspector]
        public List<NetworkObject> children = new();

        /// <summary>
        /// Parent of this object. Null if there is none.
        /// </summary>
        [HideInInspector]
        public NetworkObject Parent;
        
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
            if (!Parent)
            {
                Parent = transform.parent?.GetComponent<NetworkObject>();
                EditorUtility.SetDirty(this);
            }
                
            if (Parent)
            {
                Parent.UpdateComponents();
                return;
            }
            
            var oldComponentsCount = NetworkComponents.Count;
            var oldChildrenCount = children.Count;
            NetworkComponents.Clear();
            children.Clear();
            UpdateComponentsRecursive(transform);
            
            if(oldChildrenCount != children.Count || oldComponentsCount != NetworkComponents.Count)
                EditorUtility.SetDirty(this);
        }
        
        private void UpdateComponentsRecursive(Transform t)
        {
            if (t.gameObject.TryGetComponent(out NetworkObject otherObj))
            {
                if (otherObj != this)
                {
                    children.Add(otherObj);
                    otherObj.UpdateComponentsRecursive(otherObj.transform);
                    return;
                }
            }
            
            var networkComponents = t.GetComponents<NetworkComponent>();
            NetworkComponents.AddRange(networkComponents);
            
            foreach (Transform child in t)
                UpdateComponentsRecursive(child);
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
                
                NetworkedGraphId.OnIdAssigned(id =>
                {
                    UpdateBoundNode(NetworkProvider.Graph);
                    NetworkManager.RegisterObject(this);
                    CompleteLoadStep(ref idAssigned);
                    OnIDChanged.Invoke(this, id);
                });
            }
        }

        internal void OnSceneReady()
        {
            foreach (var networkedComponent in NetworkComponents)
                networkedComponent.OnConnected();
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
        /// Tells this object to create graph nodes associated with it and its children.
        /// </summary>
        /// <param name="graph"></param>
        internal void BuildGraph(NetworkGraph graph)
        {
            NetworkGraphNode newNode;
            NetworkId parentId = Parent ? Parent.NetworkedGraphId.Value : NetworkId.Invalid;
            if (!NetworkedGraphId.Value.IsValid())
            {
                newNode = graph.CreateNode(parentId);
                NetworkedGraphId.Value = newNode.ID;
            }
            else
                newNode = graph.AddNode(NetworkedGraphId.Value, parentId);
            
            LinkGraphNode(newNode);
            
            foreach (var child in children)
                child.BuildGraph(graph);
        }

        /// <summary>
        /// Attempts to connect to the graph node associated with this object using the NetworkedGraphId and register properties. Does nothing if the node is already connected.
        /// </summary>
        /// <param name="graph"></param>
        internal void UpdateBoundNode(NetworkGraph graph)
        {
            // If we have a node already, we don't need to do anything.
            if(associatedNode)
                return;

            if (graph.TryGetNode(NetworkedGraphId.Value, out NetworkGraphNode node))
               LinkGraphNode(node);
            else if (IsOwner && !Parent && NetworkedGraphId.Value.IsValid())
                BuildGraph(graph);
        }

        /// <summary>
        /// Set up a new node with the correct data
        /// </summary>
        /// <param name="node"></param>
        private void LinkGraphNode(NetworkGraphNode node)
        {
            Debug.Assert(node);
            node.AssociatedObject = this;
            associatedNode = node;
            node.allowOwnershipTransfer = allowOwnershipTransfer;
            Debug.Assert(node.ID == NetworkedGraphId.Value);

            node.Properties = NetworkProperties;
            
            // If there's un-serialized data for this node, consume that now that our properties have been initalized.
            node.ConsumeCachedProps();

            CompleteLoadStep(ref nodeAssigned);
        }
        
        /// <summary>
        /// Set a callback to verify if a network ID change from a client without ownership should be allowed. This will
        /// be called on every remote client, not just the owner so make sure that it's consistent between clients.
        /// </summary>
        /// <param name="callback"></param>
        public void SetVerifyIDChangeCallback(IDChangeRequestCallback callback)
        {
            IDChangeRequest = callback;
        }
        
        public bool VerifyIDChangeRequest(int sender, NetworkId newId)
        {
            if (IDChangeRequest == null)
                return allowOwnershipTransfer;
            return IDChangeRequest(sender, newId);
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
            
            if (associatedNode.ID.Owner == NetworkProvider.LocalPlayerId)
                return;
            
            NetworkProvider.Graph.ChangeId(associatedNode.ID, NetworkProvider.Graph.NewId(NetworkProvider.LocalPlayerId));
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
            EditorGUILayout.ObjectField("Parent", networkObject.Parent, typeof(NetworkObject), true);
            
            
            EditorGUILayout.LabelField("Network Components:");
            foreach (var property in networkObject.NetworkComponents)
                EditorGUILayout.ObjectField(property, typeof(NetworkComponent), true);
            if(networkObject.NetworkComponents.Count == 0)
                EditorGUILayout.LabelField("None");
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Children:");
            
            foreach (var child in networkObject.children)
                EditorGUILayout.ObjectField(child, typeof(NetworkObject), true);
            if(networkObject.children.Count == 0)
                EditorGUILayout.LabelField("None");
            EditorGUILayout.Space();
            
            EditorGUI.indentLevel = 0;
            EditorGUILayout.TextField("Guid", networkObject.guid);
            EditorGUI.EndDisabledGroup();
            
            if (Application.isPlaying && NetworkManager.instance != null)
            {
                EditorGUILayout.LabelField("Is Owner: " + networkObject.IsOwner);
                EditorGUILayout.LabelField("Networked Graph ID: " + (networkObject.NetworkedGraphId?.Value.ToString() ?? "Not set"));
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