using System;
using System.Collections.Generic;
using System.IO;
using Foundry.Core.Serialization;
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
        /// Get the serializer for this property. This is used to serialize and deserialize the property.
        /// The serializer will be passed this property to serialize and deserialize.
        /// </summary>
        public IFoundrySerializer GetSerializer();

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
        public int EventCount { get; }
        public IFoundrySerializer ArgSerializer { get; }
        public bool TryDequeue(out object eventArgs);

        public void DeserializeEvent(BinaryReader reader);
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
        
        private struct Serializer: IFoundrySerializer
        {
            public void Serialize(in object id, BinaryWriter writer)
            {
                writer.Write(((NetworkId)id).id);
            }

            public void Deserialize(ref object id, BinaryReader reader)
            {
                id = new NetworkId(reader.ReadUInt64());
            }
        }

        public IFoundrySerializer GetSerializer()
        {
            return new Serializer();
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
        public string objectId;
        
        /// <summary>
        /// What happens with this object is orphaned from it's owner.
        /// </summary>
        public DisconnectBehaviour disconnectBehaviour = DisconnectBehaviour.TransferOwnership;

        /// <summary>
        /// All the networked properties of this node. Is null if they have not been set yet.
        /// </summary>
        public List<INetworkProperty> Properties;
        public List<IFoundrySerializer> PropertySerializers;
        
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
        public void DeserializeDelta(BinaryReader reader)
        {
            Debug.Assert(Properties != null, "Properties must be set!");
            try
            {
                Debug.Assert(Properties != null, "props must be set to deserialize them!");
                UInt64 serializedProps = reader.ReadUInt64();
            
                while (serializedProps > 0)
                {
                    UInt32 propIndex = reader.ReadUInt32();
                    UInt64 propSize = reader.ReadUInt64(); // This is required for the backend, but we don't need it here.
                    object prop = Properties[(int) propIndex];
                    PropertySerializers[(int) propIndex].Deserialize(ref prop, reader);
                    Properties[(int) propIndex] = (INetworkProperty)prop;
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
                UInt64 eventCount = reader.ReadUInt64();
                while (eventCount > 0)
                {
                    --eventCount;
                    UInt32 eventType = reader.ReadUInt32();
                    Int64 eventArgSize = reader.ReadInt64();

                    if (Events.Count <= eventType)
                    {
                        Debug.LogError($"Event Index {eventType} out of range! Skipping event for {AssociatedObject.gameObject.name}.");
                        reader.BaseStream.Position += eventArgSize;
                        continue;
                    }
                    
                    Events[(int)eventType].DeserializeEvent(reader);
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

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Id.Id);
            writer.Write(owner);
            new StringSerializer().Serialize(objectId, writer);
            byte db = (byte)disconnectBehaviour;
            writer.Write(db);
            UInt64 propCount = (UInt64)Properties.Count;
            writer.Write(propCount);
            int index = 0;
            foreach (var prop in Properties)
            {
                var propSize = new UInt64Placehodler(writer);
                var propStart = (UInt64)writer.BaseStream.Position;
                PropertySerializers[index++].Serialize(prop, writer);
                var size = (UInt64)writer.BaseStream.Position - propStart;
                propSize.WriteValue(size);
            }
        }
        
        /// <summary>
        /// Deserialize this node's data from a stream, will stop before deserializing properties if properties are null.
        /// </summary>
        /// <param name="deserializer"></param>
        
        public void Deserialize(BinaryReader reader)
        {
            Id = new NetworkId(reader.ReadUInt64());
            owner = reader.ReadUInt64();
            
            object objectId = this.objectId;
            new StringSerializer().Deserialize(ref objectId, reader);
            this.objectId = (string)objectId;
            
            byte db = reader.ReadByte();
            disconnectBehaviour = (DisconnectBehaviour)db;
            if(Properties != null)
                DeserializeProperties(reader);
        }
        
        public void DeserializeProperties(BinaryReader reader)
        {
            Debug.Assert(Properties != null, "Properties must be set to deserialize an entity!");
            UInt64 propCount = reader.ReadUInt64();
            for (UInt64 i = 0; i < propCount; i++)
            {
                UInt64 propSize = reader.ReadUInt64();
                object prop = Properties[(int)i];
                PropertySerializers[(int)i].Deserialize(ref prop, reader);
                Properties[(int)i] = (INetworkProperty)prop;
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
        public bool SerializeEntityDelta(NetworkEntity node, BinaryWriter writer)
        {
            Debug.Assert(node.Properties != null, "Uncompleted entity was added to state!");
            bool hasDirtyProps = false;
            foreach (var prop in node.Properties)
            {
                if (prop.Dirty)
                {
                    hasDirtyProps = true;
                    break;
                }
            }
            
            bool unsentEvents = false;
            foreach (var ev in node.Events)
            {
                if (ev.EventCount > 0)
                {
                    unsentEvents = true;
                    break;
                }
            }

            if (hasDirtyProps || unsentEvents)
            {
                writer.Write(node.Id.Id);
                
                var dataSize = new UInt64Placehodler(writer);
                
                var writeStart = writer.BaseStream.Position;
                var dirtyProps = new UInt64Placehodler(writer);
                
                UInt64 dirtyPropsCount = 0;
                if (hasDirtyProps)
                {
                    UInt32 propIndex = 0;
                    foreach (var prop in node.Properties)
                    {
                        if (prop.Dirty)
                        {
                            ++dirtyPropsCount;
                            writer.Write(propIndex);

                            var propSize = new UInt64Placehodler(writer);
                            UInt64 propStart = (UInt64)writer.BaseStream.Position;
                            node.PropertySerializers[(int)propIndex].Serialize(prop, writer);
                            propSize.WriteValue((UInt64)writer.BaseStream.Position - propStart);
                            prop.SetClean();
                        }

                        ++propIndex;
                    }
                }
                dirtyProps.WriteValue(dirtyPropsCount);
                
                var eventCout = new UInt64Placehodler(writer);
                UInt64 serializedEvents = 0;
                if (unsentEvents)
                {
                    UInt32 eventIndex = 0;
                    foreach (var ev in node.Events)
                    {
                        ++eventIndex;
                        if (ev.EventCount == 0)
                            continue;

                        while (ev.TryDequeue(out object args))
                        {
                            writer.Write(eventIndex - 1);
                            var argSize = new UInt64Placehodler(writer);
                            var argStart = (UInt64)writer.BaseStream.Position;
                            ev.ArgSerializer.Serialize(args, writer);
                            argSize.WriteValue((UInt64)writer.BaseStream.Position - argStart);
                            ++serializedEvents;
                        }
                    }
                }

                eventCout.WriteValue(serializedEvents);

                dataSize.WriteValue((UInt64)(writer.BaseStream.Position - writeStart));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Generates a serialized delta of the network graph for all reliable properties.
        /// </summary>
        /// <returns>Delta of changed graph properties</returns>
        public void GenerateEntitiesDelta(BinaryWriter writer)
        {
            UInt64 entitiesCount = 0;
            var ecp = new UInt64Placehodler(writer);
            foreach (var node in Entities)
            {
                if (node.owner != localPlayerId)
                    continue;
            
                if(SerializeEntityDelta(node, writer))
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
        public void ApplyDelta(BinaryReader reader)
        {
            UInt64 entitiesCount = reader.ReadUInt64();
            
            for (UInt64 i = 0; i < entitiesCount; i++)
            {
                NetworkId nodeId = new NetworkId(reader.ReadUInt64());
                bool nodeFound = idToNode.TryGetValue(nodeId, out NetworkEntity node);
                if (!nodeFound)
                {
                    Debug.LogError("Unable to find node with id " + nodeId + " in state! Skipping this update.");
                    return;
                }
                node.DeserializeDelta(reader);
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
