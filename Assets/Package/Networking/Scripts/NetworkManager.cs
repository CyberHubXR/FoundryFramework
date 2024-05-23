using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using CyberHub.Brane;
using Foundry.Core.Serialization;
using Foundry.Package.Networking.Scripts;
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

        public SpawnMethod SpawnMethod;
        public Transform[] spawnPoints;

        public UnityEvent OnSessionConnected;

        [HideInInspector] public Player localPlayer;
        public HashSet<UInt64> Players = new();
        public static NetworkState State { get; private set; }

        /// <summary>
        /// network objects that are currently active in the scene
        /// </summary>
        private HashSet<NetworkObject> sceneObjects = new();

        private Dictionary<NetworkId, NetworkObject> idToObject = new();

        public bool IsSessionConnected => connected && (socket?.IsOpen ?? false);
        private bool connected = false;
        public UInt64 LocalPlayerId => State.localPlayerId;

        private FoundryWebSocket socket;

        private Dictionary<string, GameObject> prefabs;

        void Awake()
        {
            if (instance != null)
            {
                Debug.LogError("Multiple NetworkManagers detected in scene! Only one will be used.");
                Destroy(gameObject);
            }

            instance = this;
            State = null;

            var prefabAsset = Resources.Load<FoundryPrefabs>("FoundryPrefabs");
            prefabs = prefabAsset.networkedPrefabs.Select(prefab => new KeyValuePair<string, GameObject>(prefab.GetComponent<NetworkObject>().guid, prefab)).ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        async void Start()
        {
            if (autoStart)
                await StartSession();
        }

        void OnEnable()
        {
            /*if(networkProvider == null)
                networkProvider = BraneApp.GetService<INetworkProvider>();
            networkProvider.SessionConnected += OnSessionConnectedCallback;
            networkProvider.PlayerJoined += OnPlayerJoined;
            networkProvider.PlayerLeft += OnPlayerLeft;*/

            var navigator = BraneApp.GetService<ISceneNavigator>();
            navigator.NavigationStarting += OnNavigationStarting;
        }

        void OnDisable()
        {
            /*networkProvider.SessionConnected -= OnSessionConnectedCallback;
            networkProvider.PlayerJoined -= OnPlayerJoined;
            networkProvider.PlayerLeft -= OnPlayerLeft;*/

            var navigator = BraneApp.GetService<ISceneNavigator>();
            navigator.NavigationStarting -= OnNavigationStarting;

        }

        private async void OnDestroy()
        {
            await StopSession();
        }

        public void OnPlayerLeft(int player)
        {

            throw new NotImplementedException();
            /*Despawn(SpawnedPlayers[player].GetComponent<NetworkObject>());

            var id = player;
            var orphanedNodes = State.idToNode.Select(pair => pair).Where(node => node.Value.owner == id).ToList();
            foreach (var node in orphanedNodes)
            {
                if (node.Value.AssociatedObject && node.Value.AssociatedObject.disconnectBehaviour == DisconnectBehaviour.TransferOwnership)
                    State.ChangeOwner(node.Key, networkProvider.LocalPlayerId);
                else
                    State.RemoveNode(node.Key); // If the node is not associated with an object, we can just remove it, as it was probably orphaned.
            }
            SpawnedPlayers.Remove(player);*/
        }

        void OnSessionConnectedCallback()
        {
            OnSessionConnected.Invoke();
        }

        void OnNavigationStarting(ISceneNavigationEntry scene)
        {
            StopSession();
        }

        IEnumerator PollConnection()
        {
            var lastFrameTime = Time.fixedUnscaledTimeAsDouble;
            while (true)
            {
                //We yield at the beginning of the loop so that continue statements don't crash the editor.
                yield return new WaitUntil(() => Time.fixedUnscaledTimeAsDouble - lastFrameTime > 1f / 60f);
                if (!IsSessionConnected)
                    continue;
                NetworkMessage incoming = socket.ReceiveMessage();
                while (incoming != null)
                {
                    try
                    {
                        switch (incoming.Header)
                        {
                            case "spawn-entity":
                            {
                                using var deserializer = new FoundryDeserializer(incoming.Stream);
                                SpawnRemoteObject(deserializer);
                                deserializer.Dispose();
                                break;
                            }
                            case "delta-update-entities":
                            {
                                using var deserializer = new FoundryDeserializer(incoming.Stream);
                                State.ApplyDelta(deserializer);
                                deserializer.Dispose();
                                break;
                            }
                            default:
                                Debug.LogWarning("Unhandled message: " + incoming.Header);
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                    
                    incoming.Stream.Dispose();
                    incoming = socket.ReceiveMessage();
                }
                
                MemoryStream stream = new();
                FoundrySerializer serializer = new(stream);
                serializer.Serialize(in roomKey);
                State.GenerateEntitiesDelta(serializer);

                socket.SendMessage(new NetworkMessage
                {
                    Header = "delta-update-entities",
                    BodyType = WebSocketMessageType.Binary,
                    Stream = stream
                });
                serializer.Dispose();

                lastFrameTime = Time.fixedUnscaledTimeAsDouble;
            }
        }

        public async Task StartSession()
        {
            connected = false;
            var foundryConfig = BraneApp.GetConfig<FoundryCoreConfig>();
            socket = await FoundryWebSocket.Connect(foundryConfig.runtimeNetworkingServerURI);

            socket.Start();
            socket.SendMessage(NetworkMessage.FromText("enter-sector", roomKey));

            NetworkMessage enterSectorResponse = null;
            while (enterSectorResponse == null)
            {
                enterSectorResponse = socket.ReceiveMessage();
                if (enterSectorResponse == null)
                    await Task.Yield();
            }

            if (enterSectorResponse.Header != "enter-sector-res")
                throw new Exception("Expected enter-sector-res response, got " + enterSectorResponse.Header);
            
            var deserializer = new FoundryDeserializer(enterSectorResponse.Stream);

            UInt64 assignedId = 0;
            deserializer.Deserialize(ref assignedId);
            State = new NetworkState(assignedId);
            connected = true;

            bool isNewSector = false;
            deserializer.Deserialize(ref isNewSector);
            
            Players.Add(assignedId);
            
            foreach (var obj in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var netObjs = obj.GetComponentsInChildren<NetworkObject>(false);
                foreach (var netObj in netObjs)
                {
                    sceneObjects.Add(netObj);
                }

            }

            if (isNewSector)
            {
                // Register all scene objects with the server
                foreach (var obj in sceneObjects)
                {
                    try
                    {
                        
                        obj.BuildProperties();
                        SpawnLocalObject(obj);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            else
            {
                // Link up what we have locally with what the server has
                UInt64 numUsers = 0;
                deserializer.Deserialize(ref numUsers);
                for (UInt64 i = 0; i < numUsers; i++)
                {
                    UInt64 userId = 0;
                    deserializer.Deserialize(ref userId);
                    Players.Add(userId);
                }
                
                var unconnectedObjects = sceneObjects.ToDictionary(obj => obj.guid, obj => obj.gameObject);
                UInt64 numEntities = 0;
                deserializer.Deserialize(ref numEntities);
                for (UInt64 i = 0; i < numEntities; i++)
                {
                    try
                    {
                        var linked = SpawnRemoteObject(deserializer, unconnectedObjects);
                        unconnectedObjects.Remove(linked.guid);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                
                // If the scene object was not found, it must have been destroyed before we connected
                foreach (var obj in unconnectedObjects)
                    Destroy(obj.Value);
            }

            deserializer.Dispose();
            await enterSectorResponse.Stream.DisposeAsync();
            
            try
            {
                var player = Spawn(playerPrefab, Vector3.zero, Quaternion.identity);
                localPlayer = player.GetComponent<Player>();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            
            StartCoroutine(PollConnection());

            // await networkProvider.StartSessionAsync(new Foundry.Networking.SessionInfo()
            // {
            //     sessionName = RoomName(),
            //     sessionType = mode
            // });
            // Debug.Log("Network Session started.");
            //
            // networkProvider.SetSubscriberInitialStateCallback(() => State.GenerateConstructionDelta());
            //
            // // Instantiate scene graph if we're the authority
            // if (networkProvider.IsGraphAuthority)
            // {
            //     foreach (var netObj in sceneObjects)
            //         netObj.CreateState();
            // }
            //
            // await networkProvider.CompleteSceneSetup(BraneApp.GetService<ISceneNavigator>().CurrentScene);
            //
            // await networkProvider.SubscribeToStateChangesAsync((sender, delta) =>
            // {
            //     State.ApplyDelta(delta, sender);
            // });

            // Spawn a player if we're in shared mode
            /*if (mode == SessionType.Shared)
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
            }*/
        }

        public async Task StopSession()
        {
            if (!IsSessionConnected)
                return;

            //TODO send disconnect message to server

            await socket.DisposeAsync();
            //networkProvider.StopSessionAsync();
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            Debug.Assert(IsSessionConnected, "Tried to instantiate a prefab without a network session!");
            Debug.Assert(prefabs.ContainsKey(prefab.GetComponent<NetworkObject>().guid), "Prefab is not registered!");
            
            var obj = Instantiate(prefab, position, rotation);
            var netObj = obj.GetComponent<NetworkObject>();
            netObj.BuildProperties();
            SpawnLocalObject(netObj);

            return obj;
        }
        
        /// <summary>
        /// For things like scene objects, where we already have an instance of an object and entity data, and need to notify the server.
        /// </summary>
        /// <param name="entity"></param>
        private void SpawnLocalObject(NetworkObject obj)
        {
            var entity = obj.CreateEntity();
            entity.Id = State.NewId();
            Debug.Log("Spawning object with ID " + entity.Id);
            idToObject.Add(entity.Id, obj);
            SpawnEntity(entity);
            obj.InvokeConnected();
        }
        
        /// <summary>
        /// Send a spawn message to the server and add the entity to the state
        /// </summary>
        private void SpawnEntity(NetworkEntity entity)
        {
            if (!entity.Id.IsValid())
                entity.Id = State.NewId();
            entity.owner = State.localPlayerId;
            
            var stream = new MemoryStream();
            using var serializer = new FoundrySerializer(stream);
            serializer.Serialize(in roomKey);
            entity.Serialize(serializer);
             
            socket.SendMessage(new NetworkMessage
            {
                Header = "spawn-entity",
                BodyType = WebSocketMessageType.Binary,
                Stream = stream
            });
            State.AddEntity(entity);
        }

        private NetworkObject SpawnRemoteObject(FoundryDeserializer deserializer, Dictionary<string, GameObject> sceneObjects = null)
        {
            var entity = new NetworkEntity();
            entity.Deserialize(deserializer);
            GameObject prefab = null;
            sceneObjects?.TryGetValue(entity.objectId, out prefab);
            if (!prefab && prefabs.TryGetValue(entity.objectId, out prefab))
                prefab = Instantiate(prefab);
            if (!prefab)
            {
                Debug.LogError("Tried to spawn prefab with guid " + entity.objectId + " but it was not found in the prefab list.");
                return null;
            }
            var netObj = prefab.GetComponent<NetworkObject>();
            netObj.BuildProperties();
            idToObject.Add(entity.Id, netObj);
            netObj.LinkEntity(entity);
            entity.DeserializeProperties(deserializer);
            State.AddEntity(entity);
            netObj.InvokeConnected();
            return netObj;
        }
    

        public void Despawn(NetworkObject netObject)
        {
            throw new NotImplementedException();
        }
        
        /*public static void BindSceneObject(NetworkObject sceneObject)
        {
            Debug.Assert(instance, "NetworkManager instance not found! Either one does not exist or it has not been initialized yet.");
            Debug.Assert(!instance.networkProvider.IsSessionConnected, "Tried to bind a scene object after the session has started!");
            instance.sceneObjects.Add(sceneObject);
        }*/
        
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