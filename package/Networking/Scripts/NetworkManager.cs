using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Foundry.Services;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

using Random = UnityEngine.Random;

namespace Foundry.Networking
{
    
    [System.Serializable]
    public enum SpawnMethod
    {
        FixedPoint,
        Random
    }

    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager instance;

        public bool autoStart = true;
        public string roomKey = "default";
        public GameObject playerPrefab;

        private static GameObject prefabHolder;
        private static Dictionary<GameObject, GameObject> boundPrefabs;

        public SessionType networkMode = SessionType.Shared;
        
        public SpawnMethod SpawnMethod;
        public Transform[] spawnPoints;

        public UnityEvent OnSessionConnected;
        
        [HideInInspector] public Player localPlayer;
        public Dictionary<int, Player> SpawnedPlayers = new();
        
        private INetworkProvider networkProvider;
        public static NetworkState State { get; private set; }
        
        /// <summary>
        /// network objects that are currently active in the scene
        /// </summary>
        private HashSet<NetworkObject> sceneObjects = new ();
        private Dictionary<NetworkId, NetworkObject> idToObject = new ();

        private List<NetworkObject> unloadedObjects = new();
        private List<NetworkObject> objectsAwaitingLoad = new();

        void Awake()
        {
            if (instance != null)
            {
                Debug.LogError("Multiple NetworkManagers detected in scene! Only one will be used.");
                Destroy(gameObject);
            }

            instance = this;
            networkProvider = FoundryApp.GetService<INetworkProvider>();
            State = null;
            
            // If we have not baked prefabs yet for this run, do so now.
            if (prefabHolder == null)
            {
                prefabHolder = new GameObject("Foundry Prefab Holder");
                prefabHolder.SetActive(false);
                prefabHolder.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.NotEditable;
                DontDestroyOnLoad(prefabHolder);

                var prefabs = Resources.Load<FoundryPrefabs>("FoundryPrefabs");
                boundPrefabs = new();
                if(prefabs)
                {
                    foreach (var prefab in prefabs.networkedPrefabs)
                    {
                        var boundInstance = Instantiate(prefab, prefabHolder.transform);
                        networkProvider.BindNetworkObject(boundInstance, true);
                        networkProvider.RegisterPrefab(boundInstance);
                        boundPrefabs.Add(prefab, boundInstance);
                    }
                }
            }
            
            BindSceneObjects();
        }

        void Start()
        {
            if (autoStart)
                StartSession(networkMode);
        }

        void OnEnable()
        {
            if(networkProvider == null)
                networkProvider = FoundryApp.GetService<INetworkProvider>();
            networkProvider.SessionConnected += OnSessionConnectedCallback;
            networkProvider.PlayerJoined += OnPlayerJoined;
            networkProvider.PlayerLeft += OnPlayerLeft;
                
            var navigator = FoundryApp.GetService<ISceneNavigator>();
            navigator.NavigationStarting += OnNavigationStarting;
        }

        void OnDisable()
        {
            networkProvider.SessionConnected -= OnSessionConnectedCallback;
            networkProvider.PlayerJoined -= OnPlayerJoined;
            networkProvider.PlayerLeft -= OnPlayerLeft;
            
            var navigator = FoundryApp.GetService<ISceneNavigator>();
            navigator.NavigationStarting -= OnNavigationStarting;
            
        }

        private void OnDestroy()
        {
            StopSession();
        }

        public void OnPlayerJoined(int player)
        {
            if (networkProvider.IsServer)
            {
                GameObject newPlayer = this.Spawn(playerPrefab, new Vector3(), Quaternion.identity);
                SpawnedPlayers.Add(player, newPlayer.GetComponent<Player>());
            }
        }

        public void OnPlayerLeft(int player)
        {
            if (networkProvider.IsServer)
            {
                Despawn(SpawnedPlayers[player].GetComponent<NetworkObject>());
                SpawnedPlayers.Remove(player);
            }
            
            
            if (!networkProvider.IsGraphAuthority)
                return;

            var id = player;
            var orphanedNodes = State.idToNode.Select(pair => pair).Where(node => node.Value.owner == id).ToList();
            foreach (var node in orphanedNodes)
            {
                if (node.Value.AssociatedObject && node.Value.AssociatedObject.disconnectBehaviour == DisconnectBehaviour.TransferOwnership)
                    State.ChangeOwner(node.Key, networkProvider.LocalPlayerId);
                else 
                    State.RemoveNode(node.Key); // If the node is not associated with an object, we can just remove it, as it was probably orphaned.
            }
        }

        void OnSessionConnectedCallback()
        {
            OnSessionConnected.Invoke();
        }
        
        void OnGraphChanged(NetworkState state)
        {
            foreach (var obj in sceneObjects)
                obj.UpdateBoundNode();
        }

        void OnNavigationStarting(ISceneNavigationEntry scene)
        {
            StopSession();
        }

        public string RoomName()
        {
            return roomKey + SceneManager.GetActiveScene().name;
        }

        private void BindSceneObjects()
        {
            var currentSceneIndex = FoundryApp.GetService<ISceneNavigator>().CurrentScene.BuildIndex;
            var scene = SceneManager.GetSceneByBuildIndex(currentSceneIndex);
            var sceneRoots = scene.GetRootGameObjects();
            var networkObjects = new List<NetworkObject>();
            
            foreach (var root in sceneRoots)
            {
                root.GetComponentsInChildren(false, networkObjects);
                foreach(var netObj in networkObjects)
                    BindSceneObject(netObj);
            }
        }
        
        IEnumerator ReportGraphChanges()
        {
            while (networkProvider.IsSessionConnected)
            {
                //We yield at the beginning of the loop so that continue statements don't crash the editor.
                yield return new WaitForSeconds(1f / 60f);
                var graphDelta = State.GenerateDelta();
                if (graphDelta.Length == 0)
                    continue;
                
                if (graphDelta.Length > 6536)
                {
                    Debug.LogError($"Graph delta is too large to send over the network! (graphDelta was {graphDelta.Length}, max is 6536). This is a potential bug, please report it to the Foundry team.");
                    continue;
                }

                networkProvider.SendStateDelta(graphDelta);
            }
        }

        public async void StartSession(SessionType mode)
        {
            State = new NetworkState(networkProvider);
            State.OnStateStructureChanged += OnGraphChanged;
            
            await networkProvider.StartSessionAsync(new Foundry.Networking.SessionInfo()
            {
                sessionName = RoomName(),
                sessionType = mode
            });
            Debug.Log("Network Session started.");
            
            networkProvider.SetSubscriberInitialStateCallback(() => State.GenerateConstructionDelta());
            
            // Instantiate scene graph if we're the authority
            if (networkProvider.IsGraphAuthority)
            {
                foreach (var netObj in sceneObjects)
                    netObj.CreateState();
            }

            await networkProvider.CompleteSceneSetup(FoundryApp.GetService<ISceneNavigator>().CurrentScene);
            
            await networkProvider.SubscribeToStateChangesAsync((sender, delta) =>
            {
                State.ApplyDelta(delta, sender);
            });
            
            StartCoroutine(ReportGraphChanges());

            // Spawn a player if we're in shared mode
            if (mode == SessionType.Shared)
            {
                switch (SpawnMethod)
                {
                    case SpawnMethod.Random:
                        if (spawnPoints.Length > 1)
                        {
                            this.Spawn(playerPrefab, spawnPoints[Random.Range(0, spawnPoints.Length - 1)].position,
                                spawnPoints[Random.Range(0, spawnPoints.Length - 1)].rotation);
                        }
                        else
                        {
                            Debug.LogWarning("Please Define Two Or More Random Spawn Points Or This Method Will Not Function As Intended This Script Will Now Override To Fixed Point");
                            
                            //Peform Fixed Point Spawn
                            this.Spawn(playerPrefab, transform.position, transform.rotation);
                        }

                        break;
                    case SpawnMethod.FixedPoint:
                        this.Spawn(playerPrefab, transform.position, transform.rotation);
                        break;
                }
            }
        }

        public void StopSession()
        {
            if (networkProvider.IsSessionConnected)
                networkProvider.StopSessionAsync();
        }
        
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            Debug.Assert(instance.networkProvider.IsSessionConnected, "Tried to instantiate a prefab without a network session!");
            if (boundPrefabs.ContainsKey(prefab))
                prefab = boundPrefabs[prefab];
            var obj = networkProvider.Spawn(prefab, position, rotation);
            if (obj.TryGetComponent(out NetworkObject netObj))
            {
                sceneObjects.Add(netObj);
                netObj.CreateState();
            }

            return obj;
        }
        
        public void Despawn(NetworkObject netObject)
        {
            networkProvider.Despawn(netObject.gameObject);
        }
        
        public static void BindSceneObject(NetworkObject sceneObject)
        {
            Debug.Assert(instance, "NetworkManager instance not found! Either one does not exist or it has not been initialized yet.");
            Debug.Assert(!instance.networkProvider.IsSessionConnected, "Tried to bind a scene object after the session has started!");
            instance.networkProvider.BindNetworkObject(sceneObject.gameObject, false);
            instance.sceneObjects.Add(sceneObject);
        }

        /// <summary>
        /// Registers a network object with the network manager so it will receive network events. Called by network objects when they're spawned.
        /// </summary>
        /// <param name="networkObject"></param>
        public static void RegisterObject(NetworkObject networkObject)
        {
            Debug.Assert(instance, "NetworkManager instance not found! Either one does not exist or it has not been initialized yet.");
            Debug.Assert(networkObject.Id.IsValid(), "Network object must have valid ID");
            
            if(!instance.idToObject.ContainsKey(networkObject.Id))
                instance.idToObject.Add(networkObject.Id, networkObject);
            if (instance.sceneObjects.Contains(networkObject))
                return;
            
            instance.sceneObjects.Add(networkObject);
        }
        
        /// <summary>
        /// Called by network objects when they're destroyed so they can be removed from the list of active objects.
        /// </summary>
        /// <param name="networkObject"></param>
        public static void UnregisterObject(NetworkObject networkObject)
        {
            instance.sceneObjects.Remove(networkObject);
            var id = networkObject.Id;
            Debug.Assert(id.IsValid(), "Network object must have valid ID");
            instance.idToObject.Remove(id);
            if (instance.objectsAwaitingLoad.Contains(networkObject))
                instance.objectsAwaitingLoad.Remove(networkObject);
            
            if(networkObject.IsOwner)
                State?.RemoveNode(id);
        }

        public static void RegisterUnloaded(NetworkObject netObj)
        {
            instance.unloadedObjects.Add(netObj);
            instance.objectsAwaitingLoad.Add(netObj);
        }
        
        public static void RegisterLoaded(NetworkObject netObj)
        {
            Debug.Assert(instance.unloadedObjects.Contains(netObj), $"RegisterLoaded was called more than once for object {netObj.gameObject.name}");
            instance.unloadedObjects.Remove(netObj);
            if (instance.unloadedObjects.Count == 0)
            {
                foreach(var o in instance.objectsAwaitingLoad)
                    o.OnSceneReady();
                instance.objectsAwaitingLoad.Clear();
            }
        }
        
        /// <summary>
        /// Get a network object by its NetworkID 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static NetworkObject GetObjectById(NetworkId id)
        {
            if (instance.idToObject.TryGetValue(id, out var obj))
                return obj;
            return null;
        }
    }
}