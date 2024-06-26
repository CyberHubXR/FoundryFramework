using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
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

        public bool IsSessionConnected => connected && (socket?.IsOpen ?? false);
        private bool connected = false;
        public UInt64 LocalPlayerId => State.localPlayerId;
        public static int TickRate => 30;

        private FoundryWebSocket socket;

        private Dictionary<string, GameObject> prefabs;
        
        private Dictionary<NetworkId, TaskCompletionSource<bool>> ownershipChangeRequests = new();

        private UInt16 voiceChatPort = 0;
        private UInt32 voiceChannelId = UInt32.MaxValue;
        private IPEndPoint voiceChatEndpoint;
        private UdpClient voiceChatClient;
        private Dictionary<UInt64, Action<UInt64, ArraySegment<byte>>> voiceChatListeners = new();

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
            var rk = roomKey;
            var sock = socket;
            await StopSessionInternal(sock, rk);
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

        IEnumerator SendDelta()
        {
            var lastFrameTime = Time.unscaledTimeAsDouble;
            var lastPrintTime = Time.unscaledTimeAsDouble;
            int ticks_this_frame = 0;
            while (IsSessionConnected)
            {
                yield return new WaitUntil(() => Time.unscaledTimeAsDouble - lastFrameTime >= 1f / TickRate);
                ticks_this_frame += 1;
                if (Time.unscaledTimeAsDouble - lastPrintTime >= 1f)
                {
                    ticks_this_frame = 0;
                    lastPrintTime = Time.unscaledTimeAsDouble;
                }
                
                
                MemoryStream stream = new();
                using var writer = new BinaryWriter(stream);
                new StringSerializer().Serialize(roomKey, writer);
                State.GenerateEntitiesDelta(writer);

                socket.SendMessage(new NetworkMessage
                {
                    Header = "delta-update-entities",
                    BodyType = WebSocketMessageType.Binary,
                    Stream = stream
                });

                lastFrameTime = Time.unscaledTimeAsDouble;
            }
        }

        public async Task StartSession()
        {
            connected = false;
            var foundryConfig = BraneApp.GetConfig<FoundryCoreConfig>();
            socket = await FoundryWebSocket.Connect(new Uri(foundryConfig.runtimeNetworkingServerURI));

            socket.Start();
            socket.SendMessage(NetworkMessage.FromText("enter-sector", roomKey));

            NetworkMessage enterSectorResponse = null;
            while (enterSectorResponse == null)
            {
                await Task.Yield();
                enterSectorResponse = socket.ReceiveMessage();
                if (enterSectorResponse == null)
                    continue;
                if (enterSectorResponse.Header != "enter-sector-res")
                {
                    Debug.LogWarning("Received unexpected message: " + enterSectorResponse.Header);
                    enterSectorResponse = null;
                }
            }

            if (enterSectorResponse.Header != "enter-sector-res")
                throw new Exception("Expected enter-sector-res response, got " + enterSectorResponse.Header);
            
            using var reader = new BinaryReader(enterSectorResponse.Stream);

            UInt64 assignedId = reader.ReadUInt64();
            State = new NetworkState(assignedId);
            connected = true;

            bool isNewSector = reader.ReadBoolean();
            
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
                UInt64 numUsers = reader.ReadUInt64();
                for (UInt64 i = 0; i < numUsers; i++)
                {
                    UInt64 userId = reader.ReadUInt64();
                    Players.Add(userId);
                }
                
                var unconnectedObjects = sceneObjects.ToDictionary(obj => obj.guid, obj => obj.gameObject);
                UInt64 numEntities = reader.ReadUInt64();
                for (UInt64 i = 0; i < numEntities; i++)
                {
                    try
                    {
                        var linked = SpawnRemoteObject(reader, unconnectedObjects);
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

            await enterSectorResponse.Stream.DisposeAsync();
            
            try
            {
                GameObject player = null;
                switch (SpawnMethod)
                {
                    case SpawnMethod.Random:
                        if (spawnPoints.Length > 1)
                        {
                            var point = spawnPoints[Random.Range(0, spawnPoints.Length - 1)];
                            player = Spawn(playerPrefab, point.position, point.rotation);
                        }
                        else
                        {
                            Debug.LogWarning("Please Define Two Or More Random Spawn Points Or This Method Will Not Function As Intended This Script Will Now Override To Fixed Point");

                            //Peform Fixed Point Spawn
                            player = Spawn(playerPrefab, transform.position, transform.rotation);
                        }

                        break;
                    case SpawnMethod.FixedPoint:
                        player = Spawn(playerPrefab, transform.position, transform.rotation);
                        break;
                }
                localPlayer = player.GetComponent<Player>();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            
            var vcJoinStream = new MemoryStream();
            using var vcJoinWriter = new BinaryWriter(vcJoinStream);
            new StringSerializer().Serialize(roomKey, vcJoinWriter);

            voiceChatEndpoint = new(socket.GetIP(), 9090);

            // If two people are playing on the same router, it may confuse packets if they are on the same port
            voiceChatPort = (ushort)Random.Range(49152, 65536);

            int maxTries = 10;
            while (voiceChatClient == null && maxTries-- > 0)
            {
                try
                {
                    voiceChatClient = new UdpClient(voiceChatPort);
                    voiceChatClient.Connect(voiceChatEndpoint);
                }
                catch (SocketException e)
                {
                    Debug.LogException(e);
                    voiceChatPort = (ushort)Random.Range(49152, 65536);
                    voiceChatClient = null;
                }
            }
            if(maxTries == 0 && voiceChatClient == null)
                throw new Exception("Failed to bind to voice chat port after 10 tries.");
            vcJoinWriter.Write(voiceChatPort);

            socket.SendMessage(new NetworkMessage
            {
                BodyType = WebSocketMessageType.Binary,
                Header = "join-sector-voice",
                Stream = vcJoinStream
            });

            Task.Run(VoiceListenService);
            StartCoroutine(ProcessMessages());
            StartCoroutine(SendDelta());
        }

        public async Task StopSession()
        {
            if (!IsSessionConnected)
                return;

            await StopSessionInternal(socket, roomKey);
        }
        
        public static async Task StopSessionInternal(FoundryWebSocket socket,  string roomKey)
        {
            socket.SendMessage(NetworkMessage.FromText("exit-sector", roomKey));
            await socket.AwaitAllSent();
            await socket.DisposeAsync();
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
            using var writer = new BinaryWriter(stream);
            object roomKey = this.roomKey;
            new StringSerializer().Serialize(roomKey, writer);
            entity.Serialize(writer);
             
            socket.SendMessage(new NetworkMessage
            {
                Header = "spawn-entity",
                BodyType = WebSocketMessageType.Binary,
                Stream = stream
            });
            State.AddEntity(entity);
        }

        private NetworkObject SpawnRemoteObject(BinaryReader reader, Dictionary<string, GameObject> sceneObjects = null)
        {
            var entity = new NetworkEntity();
            entity.Deserialize(reader);
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
            netObj.LinkEntity(entity);
            entity.DeserializeProperties(reader);
            State.AddEntity(entity);
            netObj.InvokeConnected();
            return netObj;
        }
    

        public void Despawn(NetworkObject netObject)
        {
            if (!IsSessionConnected)
            {
                Destroy(netObject.gameObject);
            }
            
            var entity = netObject.Entity;
            if (entity == null)
            {
                Debug.LogWarning("Tried to despawn object with no entity.");
                return;
            }
            if (entity.owner != State.localPlayerId)
            {
                Debug.LogWarning("Tried to despawn object with ID " + entity.Id + " but we are not the owner.");
                return;
            }
            
            var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(entity.Id.Id);
            socket.SendMessage(new NetworkMessage
            {
                Header = "despawn-entity",
                BodyType = WebSocketMessageType.Binary,
                Stream = stream
            });
            State.RemoveNode(entity.Id);
            Destroy(netObject);
        }
        
        /// <summary>
        /// Get a network object by its NetworkID 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static NetworkObject GetObjectById(NetworkId id)
        {
            if (State.idToNode.TryGetValue(id, out var obj))
                return obj.AssociatedObject;
            return null;
        }
        
        

        internal static async Task<bool> RequestObjectOwnership(NetworkObject obj)
        {
            Debug.Assert(instance, "No network manager instance found!");
            if (!instance.IsSessionConnected)
                return true;
            
            var entity = obj.Entity;
            if (entity.owner == State.localPlayerId)
                return true;
            
            var stream = new MemoryStream();
            await using var writer = new BinaryWriter(stream);
            object roomKey = instance.roomKey;
            new StringSerializer().Serialize(roomKey, writer);
            writer.Write(entity.Id.Id);
            instance.socket.SendMessage(new NetworkMessage
            {
                Header = "request-entity-ownership",
                BodyType = WebSocketMessageType.Binary,
                Stream = stream
            });
            entity.owner = State.localPlayerId;
            
            TaskCompletionSource<bool> listener = new();
            instance.ownershipChangeRequests[entity.Id] = listener;
            
            return await listener.Task;
        }

        private IEnumerator ProcessMessages()
        {
            while (IsSessionConnected)
            {
                voiceMutex.WaitOne();
                while (voicePackets.TryDequeue(out byte[] voiceData))
                {
                    UInt32 channelId = BitConverter.ToUInt32(new ReadOnlySpan<byte>(voiceData, 0, 4));
                    if (channelId != voiceChannelId)
                        Debug.LogWarning("Received voice chat packet for channel " + channelId + " but we are listening on channel " + voiceChannelId);
                    UInt64 userId = BitConverter.ToUInt64(new ReadOnlySpan<byte>(voiceData,  4,8));
                    UInt64 index = BitConverter.ToUInt64(new ReadOnlySpan<byte>(voiceData, 12, 8));
                    if (voiceChatListeners.TryGetValue(userId, out var listener))
                    {
                        try
                        {
                            listener(index, new ArraySegment<byte>(voiceData, 20, 256));
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
                voiceMutex.ReleaseMutex();
                
                NetworkMessage incoming = socket.ReceiveMessage();
                while (incoming != null)
                {
                    try
                    {
                        switch (incoming.Header)
                        {
                            case "spawn-entity":
                            {
                                using var reader = incoming.AsReader();
                                SpawnRemoteObject(reader);
                                break;
                            }
                            case "delta-update-entities":
                            {
                                using var reader = incoming.AsReader();
                                State.ApplyDelta(reader);
                                break;
                            }
                            case "change-entity-owner":
                            {
                                using var reader = incoming.AsReader();
                                string sectorKey = "";
                                object sectorKeyObj = sectorKey;
                                new StringSerializer().Deserialize(ref sectorKeyObj, reader);
                                sectorKey = (string) sectorKeyObj;
                                if (sectorKey != roomKey)
                                {
                                    Debug.LogWarning("Received ownership change for object in sector " + sectorKey + " but we are in sector " + roomKey);
                                    continue;
                                }
                                NetworkId id = new NetworkId(reader.ReadUInt64());
                                UInt64 newOwner = reader.ReadUInt64();
                                if (State.idToNode.TryGetValue(id, out var e))
                                {
                                    e.owner = newOwner;
                                    var resetProps = true;
                                    if (ownershipChangeRequests.TryGetValue(id, out var tcs))
                                    {
                                        var success = newOwner == State.localPlayerId;
                                        resetProps = !success;
                                        tcs.SetResult(success);
                                        ownershipChangeRequests.Remove(id);
                                    }
                                    
                                    if (resetProps)
                                        e.DeserializeProperties(reader);
                                }
                                else
                                {
                                    Debug.LogWarning("Tried to change owner of object with ID " + id + " but it was not found.");
                                }

                                break;
                            }
                            case "request-entity-ownership":
                            {
                                using var reader = incoming.AsReader();
                                NetworkId id = new NetworkId(reader.ReadUInt64());
                                if (!State.idToNode.TryGetValue(id, out var e))
                                    continue;
                                
                                UInt64 newOwner = reader.ReadUInt64();
                                if (e.AssociatedObject && e.AssociatedObject.VerifyIDChangeRequest(newOwner))
                                {
                                    e.owner = newOwner;
                                }
                                else
                                    newOwner = State.localPlayerId;
                                
                                var ostream = new MemoryStream();
                                using var w = new BinaryWriter(ostream);
                                new StringSerializer().Serialize(roomKey, w);
                                w.Write(id.Id);
                                w.Write(newOwner);
                                    
                                socket.SendMessage(new NetworkMessage
                                {
                                    Header = "change-entity-owner",
                                    BodyType = WebSocketMessageType.Binary,
                                    Stream = ostream
                                });

                                break;
                            }
                            case "despawn-entity":
                            {
                                using var reader = incoming.AsReader();
                                NetworkId id = new NetworkId(reader.ReadUInt64());
                                if (State.idToNode.TryGetValue(id, out var entity))
                                {
                                    if(entity.AssociatedObject)
                                        Destroy(entity.AssociatedObject.gameObject);
                                    State.RemoveNode(id);
                                }
                                else
                                {
                                    Debug.LogWarning("Tried to destroy object with ID " + id + " but it was not found.");
                                }
                                break;
                            }
                            case "user-entered-sector":
                            {
                                using var reader = incoming.AsReader();
                                UInt64 userId = reader.ReadUInt64();
                                string sectorName = "";
                                object sectorNameObj = sectorName;
                                new StringSerializer().Deserialize(ref sectorNameObj, reader);
                                sectorName = (string) sectorNameObj;
                                Players.Add(userId);
                                Debug.Assert(sectorName == roomKey, "Got event for user entered sector " + sectorName + " but we are in sector " + roomKey);
                                break;
                            }
                            case "user-left-sector":
                            {
                                using var reader = incoming.AsReader();
                                UInt64 userId = reader.ReadUInt64();
                                string sectorName = "";
                                object sectorNameObj = sectorName;
                                new StringSerializer().Deserialize(ref sectorNameObj, reader);
                                sectorName = (string) sectorNameObj;
                                Players.Remove(userId);
                                Debug.Assert(sectorName == roomKey, "Got event for user left sector " + sectorName + " but we are in sector " + roomKey);
                                break;
                            }
                            case "join-sector-voice":
                            {
                                using var reader = incoming.AsReader();
                                voiceChannelId = reader.ReadUInt32();
                                break;
                            }
                            case "error":
                            {
                                using var reader = new StreamReader(incoming.Stream);
                                Debug.LogError("Server error: " + reader.ReadToEnd());
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
                yield return null;
            }
        }
        
        private Mutex voiceMutex = new();
        private Queue<byte[]> voicePackets = new();
        async Task VoiceListenService()
        {
            while (IsSessionConnected)
            {
                try
                {
                    var result = await voiceChatClient.ReceiveAsync();
                    if (result.Buffer.Length > 0)
                    {

                        if (result.Buffer.Length != 276)
                        {
                            Debug.LogWarning("Received voice chat packet of unexpected length: " + result.Buffer.Length);
                            continue;
                        }
                        voiceMutex.WaitOne();
                        voicePackets.Enqueue(result.Buffer);
                        voiceMutex.ReleaseMutex();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
        
        public void RegisterVoiceChatListener(UInt64 userId, Action<UInt64, ArraySegment<byte>> listener)
        {
            voiceChatListeners[userId] = listener;
        }
        
        public void UnregisterVoiceChatListener(UInt64 userId)
        {
            voiceChatListeners.Remove(userId);
        }
        
        public async Task SendVoiceChatPacket(UInt64 index, byte[] data)
        {
            if (!IsSessionConnected || voiceChannelId == UInt32.MaxValue)
                return;
            var message = new byte[276];
            BitConverter.TryWriteBytes(new Span<byte>(message, 0, 4), voiceChannelId);
            BitConverter.TryWriteBytes(new Span<byte>(message, 4, 8), State.localPlayerId);
            BitConverter.TryWriteBytes(new Span<byte>(message, 12, 8), index);
            data.CopyTo(new Span<byte>(message, 20, 256));
            await voiceChatClient.SendAsync(message, 276);
        }
    }
}