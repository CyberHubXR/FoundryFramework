using System;
using System.Collections.Generic;
using System.IO;
using Foundry.Core.Serialization;
using UnityEditor;
using UnityEngine;


namespace Foundry.Networking
{
    /// <summary>
    /// Interface for all networked properties. A networked property is usually a value synced across the network.
    /// </summary>
    public interface INetworkProperty
    {
        /// <summary>
        /// If dirty, this property will be synced across the network.
        /// </summary>
        public bool Dirty { get; }
        
        /// <summary>
        /// Set this property dirty, useful for when a property is synced incrementally and we need to send the whole object.
        /// </summary>
        public void SetDirty();
        
        /// <summary>
        /// Called after variable has been serialized and sent across the network to reset state.
        /// </summary>
        public void SetClean();

        /// <summary>
        /// Write contained data out to the stream to be synced across the network.
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="full">If true, serialize all data, otherwise only serialize dirty data.</param>
        public void Serialize(FoundrySerializer serializer);

        /// <summary>
        /// Read data received from the network into this property.
        /// </summary>
        /// <param name="deserializer"></param>
        public void Deserialize(FoundryDeserializer deserializer);

        /// <summary>
        /// Should print a useful string representation of this property for debugging.
        /// </summary>
        /// <returns></returns>
        public string ToString();

        /// <summary>
        /// Generic change event for when the property has been changed, either locally or remotely.
        /// We recommend using a more granular event provided directly by whatever class is implementing this property.
        /// </summary>
        public Action OnChanged { get; set; }
    }

    public interface INetworkEvent
    {
        public bool Dirty { get; }
        public Queue<byte[]> SerializeEventQueue();

        public void DeserializeEvent(byte[] args);
    }

    [Serializable]
    public struct NetworkId : IFoundrySerializable
    {
        /// <summary>
        /// Locally unique id of this object
        /// </summary>
        public UInt64 Id => id;
        private UInt64 id;
        
        public uint Creator => (uint) (id >> 32);
        public uint Index => (uint)id;
        
        public NetworkId(ulong rawId)
        {
            this.id = rawId;
        }
        
        public NetworkId(UInt64 creator, uint index)
        {
            this.id = creator << 32 | index;
        }
        
        public static NetworkId Invalid => new(0xffffffffffffffff);
        
        public bool IsValid() =>Id != 0xffffffffffffffff;
        
        /// <summary>
        /// Override of hashing function to allow for use in dictionaries
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        public override string ToString()
        {
            if(!IsValid())
                return "NetworkId(Invalid)";
            return $"NetworkId(Created By: {Creator} ID = {Index}, Raw = {id})";
        }

        public void Serialize(FoundrySerializer serializer)
        {
            serializer.Serialize(in id);
        }

        public void Deserialize(FoundryDeserializer deserializer)
        {
            deserializer.Deserialize(ref id);
        }

        public static bool operator ==(NetworkId a, NetworkId b)
        {
            return a.id == b.id;
        }
        
        public static bool operator !=(NetworkId a, NetworkId b)
        {
            return a.id != b.id;
        }
    }
    
    public class NetworkEntity
    {
        public NetworkId Id = NetworkId.Invalid;

        /// <summary>
        /// The object in the scene associated with this node. This may be null if this node does not represent a scene object.
        /// </summary>
        public NetworkObject AssociatedObject;

        /// <summary>
        /// The prefab or scene object linked to this entity.
        /// </summary>
        public String objectId;
        
        /// <summary>
        /// What happens with this object is orphaned from it's owner.
        /// </summary>
        public DisconnectBehaviour disconnectBehaviour = DisconnectBehaviour.TransferOwnership;
        
        /// <summary>
        /// Used to check if incoming ownership transfer requests are valid.
        /// </summary>
        public bool allowOwnershipTransfer = false;

        /// <summary>
        /// All the networked properties of this node. Is null if they have not been set yet.
        /// </summary>
        public List<INetworkProperty> Properties;
        
        /// <summary>
        /// All the networked events of this node.
        /// </summary>
        public List<INetworkEvent> Events;
        
        /// <summary>
        /// The client with authority over this node. UInt32.MaxValue if no one has authority.
        /// </summary>
        public UInt64 owner = UInt64.MaxValue;

        /// <summary>
        /// If this node is still alive in the graph
        /// </summary>
        public bool IsAlive = true;
        
        public static implicit operator bool(NetworkEntity node) => node?.IsAlive ?? false;

        /// <summary>
        /// Deserialize this node's data from a stream
        /// </summary>
        /// <param name="deserializer"></param>
        public void DeserializeDelta(FoundryDeserializer deserializer)
        {
            Debug.Assert(Properties != null, "Properties must be set!");
            try
            {
                deserializer.SetDebugRegion("prop count");
                Debug.Assert(Properties != null, "props must be set to deserialize them!");
                UInt64 serializedProps = 0;
                deserializer.Deserialize(ref serializedProps);
            
                while (serializedProps > 0)
                {
                    UInt32 propIndex = 0;
                    deserializer.SetDebugRegion("prop index");
                    deserializer.Deserialize(ref propIndex);

                    UInt64 propSize = 0;
                    deserializer.SetDebugRegion("prop size");
                    deserializer.Deserialize(ref propSize);
                    
                    deserializer.SetDebugRegion("prop data");
                    Properties[(int) propIndex].Deserialize(deserializer);
                    --serializedProps;
                }
                
            }
            catch (Exception e)
            {
                Debug.LogError("Error deserializing properties for node " + Id + ": " + e);
                // Attempt to recover by skipping the rest of the data
                #if UNITY_EDITOR
                throw;
                #endif
            }
            
            try 
            {
                deserializer.SetDebugRegion("event count");
                UInt64 eventCount = 0;
                deserializer.Deserialize(ref eventCount);
                while (eventCount > 0)
                {
                    --eventCount;
                    deserializer.SetDebugRegion("event");
                    UInt32 eventType = 0;
                    deserializer.Deserialize(ref eventType);
                    UInt64 eventArgSize = 0;
                    deserializer.Deserialize(ref eventArgSize);
                    var arg = new byte[eventArgSize];
                    for (UInt64 i = 0; i < eventArgSize; i++)
                        deserializer.Deserialize(ref arg[i]);
                    
                    Events[(int)eventType].DeserializeEvent(arg);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error deserializing events for node " + Id + ": " + e);
                // Attempt to recover by skipping the rest of the data
                #if UNITY_EDITOR
                throw;
                #endif
            }
        }

        /// <summary>
        /// Skip deserializing this node's data from a stream, used when we want to ignore an update to this node.
        /// </summary>
        /// <param name="deserializer"></param>
        public static void Skip(FoundryDeserializer deserializer)
        {
            deserializer.SetDebugRegion("Skip Node");
            UInt32 propsDataSize = 0;
            deserializer.Deserialize(ref propsDataSize);
            var streamPos = deserializer.stream.Position;
            deserializer.stream.Position = streamPos + propsDataSize;
        }

        public void Serialize(FoundrySerializer serializer)
        {
            serializer.Serialize(in Id);
            serializer.Serialize(in owner);
            serializer.Serialize(in objectId);
            byte db = (byte)disconnectBehaviour;
            serializer.Serialize(in db);
            UInt64 propCount = (UInt64)Properties.Count;
            serializer.Serialize(in propCount);
            foreach (var prop in Properties)
            {
                var propSize = serializer.GetPlaceholder<UInt64>(0);
                var propStart = (UInt64)serializer.stream.Position;
                prop.Serialize(serializer);
                var size = (UInt64)serializer.stream.Position - propStart;
                propSize.WriteValue(size);
            }
        }
        
        /// <summary>
        /// Deserialize this node's data from a stream, will stop before deserializing properties if properties are null.
        /// </summary>
        /// <param name="deserializer"></param>
        
        public void Deserialize(FoundryDeserializer deserializer)
        {
            deserializer.Deserialize(ref Id);
            deserializer.Deserialize(ref owner);
            deserializer.Deserialize(ref objectId);
            byte db = 0;
            deserializer.Deserialize(ref db);
            disconnectBehaviour = (DisconnectBehaviour)db;
            if(Properties != null)
                DeserializeProperties(deserializer);
        }
        
        public void DeserializeProperties(FoundryDeserializer deserializer)
        {
            Debug.Assert(Properties != null, "Properties must be set to deserialize an entity!");
            UInt64 propCount = 0;
            deserializer.Deserialize(ref propCount);
            for (UInt64 i = 0; i < propCount; i++)
            {
                UInt64 propSize = 0;
                deserializer.Deserialize(ref propSize);
                Properties[(int)i].Deserialize(deserializer);
            }
        }
    }

    public class NetworkState
    {
        public UInt64 localPlayerId;
        
        public List<NetworkEntity> Entities = new();
        public Dictionary<NetworkId, NetworkEntity> idToNode { get; } = new();

        /// <summary>
        /// Local counter for id generation
        /// </summary>
        private UInt32 nextId = 0;
        
        public NetworkState(UInt64 localPlayerId)
        {
            this.localPlayerId = localPlayerId;
        }

        public NetworkId NewId()
        {
            //Use the local player id as part of the id to ensure uniqueness across the network, this will work as long as you have less than 65536 players or objects. (Which at that point, you have bigger problems)
            return new NetworkId(localPlayerId, nextId++);
        }
        
        public void AddEntity(NetworkEntity node)
        {
            idToNode[node.Id] = node;
            Entities.Add(node);
        }

        public NetworkEntity CreateEntity(NetworkId id, UInt32 owner)
        {
            Debug.Log($"Adding node {id} with owner {owner}");
            // If we receive a node with an id that already exists, this is most likely a duplicate message, so for now we will just ignore it.
            if (idToNode.TryGetValue(id, out var existing))
            {
                Debug.LogWarning("Received creation event duplicate for node with id " + id + ", ignoring.");
                return existing;
            }
            
            NetworkEntity node = new NetworkEntity
            {
                Id = id,
                owner = owner
            };
            
            idToNode[node.Id] = node;
            Entities.Add(node);
            return node;
        }
        
        public void RemoveNode(NetworkId id)
        {
            Debug.Assert(id.IsValid(), "Cannot remove node with invalid id!");
            var node = idToNode[id];
            
            Entities.Remove(node);
            idToNode.Remove(id);
            node.IsAlive = false;
        }
        
        /// <summary>
        /// Change the owner of a node
        /// </summary>
        /// <param name="id"></param>
        /// <param name="newOwner"></param>
        /// <param name="recordEvent"></param>
        public void ChangeOwner(NetworkId id, UInt32 newOwner)
        {
            var node = idToNode[id];
            node.owner = newOwner;
        }
        
        /// <summary>
        /// Attempt to get a node by its id, returns false if the node does not exist.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool TryGetNode(NetworkId id, out NetworkEntity node)
        {
            return idToNode.TryGetValue(id, out node);
        }

        /// <summary>
        /// Returns true if the client owns the given node, or is the graph authority.
        /// </summary>
        /// <param name="client">Client to check</param>
        /// <param name="id">Id of the node in question</param>
        /// <returns></returns>
        public bool ClientHasAuthority(UInt64 client, NetworkEntity node)
        {
            return node.owner == client || client == 0;
        }

        /// <summary>
        /// Serialize a node tree, starting at the given node, recursively including all of its children
        /// </summary>
        /// <param name="node">Node to begin at</param>
        /// <param name="serializer"></param>
        /// <param name="serializeAll"></param>
        /// <returns>If this node was serialized</returns>
        public bool SerializeEntityDelta(NetworkEntity node, FoundrySerializer serializer, bool serializeAll = false)
        {
            serializer.SetDebugRegion("Serialize Node");
            Debug.Assert(node.Properties != null, "Uncompleted entity was added to state!");
            UInt64 dirtyProps = 0;
            foreach (var prop in node.Properties)
            {
                if (prop.Dirty || serializeAll)
                    ++dirtyProps;
            }

            if (dirtyProps > 0 || serializeAll)
            {
                serializer.SetDebugRegion("node id");
                serializer.Serialize(in node.Id);
                
                serializer.SetDebugRegion("data size");
                var dataSize = serializer.GetPlaceholder<UInt64>(0);
                
                var writeStart = serializer.stream.Position;
                serializer.SetDebugRegion("prop count");
                serializer.Serialize(in dirtyProps);
                UInt32 propIndex = 0;
                foreach (var prop in node.Properties)
                {
                    if (prop.Dirty || serializeAll)
                    {
                        serializer.SetDebugRegion("prop index");
                        serializer.Serialize(in propIndex);

                        var propSize = serializer.GetPlaceholder<UInt64>(0);
                        UInt64 propStart = (UInt64)serializer.stream.Position;

                        serializer.SetDebugRegion("prop data");
                        prop.Serialize(serializer);
                        propSize.WriteValue((UInt64)serializer.stream.Position - propStart);
                        
                        if(!serializeAll)
                            prop.SetClean();
                    }
                    ++propIndex;
                }
                
                var eventCout = serializer.GetPlaceholder<UInt64>(0);
                UInt32 eventIndex = 0;
                UInt64 serializedEvents = 0;
                foreach(var ev in node.Events)
                {
                    ++eventIndex;
                    if(!ev.Dirty)
                        continue;
                    
                    var eventQueue = ev.SerializeEventQueue();
                    if (eventQueue.Count == 0)
                        continue;
                    
                    while(eventQueue.Count > 0)
                    {
                        var eventArg = eventQueue.Dequeue();
                        serializer.SetDebugRegion("event");
                        serializer.Serialize(eventIndex);
                        serializer.Serialize((UInt64)eventArg.Length);
                        foreach (var b in eventArg)
                            serializer.Serialize(b);
                        ++serializedEvents;
                    }
                }
                eventCout.WriteValue(serializedEvents);

                serializer.SetDebugRegion("data size");
                dataSize.WriteValue((UInt64)(serializer.stream.Position - writeStart));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Generates a serialized delta of the network graph for all reliable properties.
        /// </summary>
        /// <returns>Delta of changed graph properties</returns>
        public void GenerateEntitiesDelta(FoundrySerializer serializer)
        {
            UInt64 entitiesCount = 0;
            var ecp = serializer.GetPlaceholder(entitiesCount);
            foreach (var node in Entities)
            {
                if (node.owner != localPlayerId)
                    continue;
            
                if(SerializeEntityDelta(node, serializer))
                    entitiesCount++;
            }
            ecp.WriteValue(entitiesCount);
        }

        /// <summary>
        /// Applies the changes recorded in a delta to the network graph.
        /// </summary>
        /// <param name="delta">serialized data</param>
        /// <param name="sender">the client that sent this delta, used for determining if the updates sent are authorized</param>
        /// <param name="clearOnFullGraph">If true, the graph will be cleared and rebuilt if a full graph is received</param>
        /// <returns>Returns true if the graph was applied successfully</returns>
        public void ApplyDelta(FoundryDeserializer  deserializer)
        {
            deserializer.SetDebugRegion("Apply Delta Size");
            UInt64 entitiesCount = 0;
            deserializer.Deserialize(ref entitiesCount);
            
            for (UInt64 i = 0; i < entitiesCount; i++)
            {
                deserializer.SetDebugRegion("Deserialize node");
                NetworkId nodeId = NetworkId.Invalid;
                deserializer.Deserialize(ref nodeId);
                bool nodeFound = idToNode.TryGetValue(nodeId, out NetworkEntity node);
                if (!nodeFound)
                {
                    Debug.LogError("Unable to find node with id " + nodeId + " in state! Skipping this update.");
                    return;
                }
                node.DeserializeDelta(deserializer);
            }
        }
    }

    /// <summary>
    /// Serializable object representing a change in the network graph.
    /// </summary>
    [System.Serializable]
    public struct NetworkGraphDelta
    {
        public byte[] data;
    }
}
