using System;
using System.Collections.Generic;
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
            //TODO fix both of these to actually work
            if (networkProvider.IsServer)
            {
                GameObject newPlayer = this.Instantiate(playerPrefab, new Vector3(), Quaternion.identity);
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
        }

        void OnSessionConnectedCallback()
        {
            networkProvider.Graph.OnGraphChanged += OnGraphChanged;
            OnSessionConnected.Invoke();
        }
        
        void OnGraphChanged(NetworkGraph graph)
        {
            foreach (var obj in sceneObjects)
                obj.UpdateBoundNode(graph);
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

        public async void StartSession(SessionType mode)
        {
            
            await networkProvider.StartSessionAsync(new Foundry.Networking.SessionInfo()
            {
                sessionName = RoomName(),
                sessionType = mode
            });

            // Instantiate scene graph if we're the authority
            if (networkProvider.IsGraphAuthority)
            {
                foreach (var netObj in sceneObjects)
                {
                    netObj.BuildGraph(networkProvider.Graph);
                }
            }

            await networkProvider.CompleteSceneSetup(FoundryApp.GetService<ISceneNavigator>().CurrentScene);

            if(mode == SessionType.Shared)
                switch (SpawnMethod)
                {
                    case SpawnMethod.Random:
                        if (spawnPoints.Length > 1)
                        {
                            this.Instantiate(playerPrefab, spawnPoints[Random.Range(0, spawnPoints.Length - 1)].position,
                                spawnPoints[Random.Range(0, spawnPoints.Length - 1)].rotation);
                        }
                        else
                        {
                            Debug.LogWarning("Please Define Two Or More Random Spawn Points Or This Method Will Not Function As Intended This Script Will Now Override To Fixed Point");
                            
                            //Peform Fixed Point Spawn
                            this.Instantiate(playerPrefab, transform.position, transform.rotation);
                        }

                        break;
                    case SpawnMethod.FixedPoint:
                        this.Instantiate(playerPrefab, transform.position, transform.rotation);
                        break;
                }
        }

        public void StopSession()
        {
            if (networkProvider.IsSessionConnected)
            {
                if(networkProvider.Graph != null)
                    networkProvider.Graph.OnGraphChanged -= OnGraphChanged;
                networkProvider.StopSessionAsync();
            }
        }
        
        public GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            Debug.Assert(instance.networkProvider.IsSessionConnected, "Tried to instantiate a prefab without a network session!");
            if (boundPrefabs.ContainsKey(prefab))
                prefab = boundPrefabs[prefab];
            var obj = networkProvider.Instantiate(prefab, position, rotation);
            if (obj.TryGetComponent(out NetworkObject netObj))
            {
                sceneObjects.Add(netObj);
                netObj.BuildGraph(networkProvider.Graph);
            }

            return obj;
        }
        
        public void Despawn(NetworkObject gameObject)
        {
            Despawn(gameObject);
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
            if(!instance.idToObject.ContainsKey(networkObject.NetworkedGraphId.Value))
                instance.idToObject.Add(networkObject.NetworkedGraphId.Value, networkObject);
            if (instance.sceneObjects.Contains(networkObject))
                return;
            
            instance.sceneObjects.Add(networkObject);
            Debug.Assert(networkObject.NetworkedGraphId.Value.IsValid(), "Network object must have valid ID");
        }
        
        /// <summary>
        /// Called by network objects when they're destroyed so they can be removed from the list of active objects.
        /// </summary>
        /// <param name="networkObject"></param>
        public static void UnregisterObject(NetworkObject networkObject)
        {
            instance.sceneObjects.Remove(networkObject);
            var id = networkObject.NetworkedGraphId.Value;
            Debug.Assert(id.IsValid(), "Network object must have valid ID");
            instance.idToObject.Remove(id);
            if (instance.objectsAwaitingLoad.Contains(networkObject))
                instance.objectsAwaitingLoad.Remove(networkObject);
            
            // If this is a root object, remove it and it's children from the graph
            if(networkObject.IsOwner && !networkObject.Parent)
                instance.networkProvider.Graph.RemoveNode(id);
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