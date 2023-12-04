using System;
using System.Collections.Generic;
using System.IO;
using ExitGames.Client.Photon.StructWrapping;
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
        public void Serialize(FoundrySerializer serializer, bool full = false);

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

    [Serializable]
    public struct NetworkId : IFoundrySerializable
    {
        /// <summary>
        /// Locally unique id of this object
        /// </summary>
        public uint Id => id;
        private uint id;
        
        public int Creator => (int) (id >> 16);
        public uint Index => id & 0xffff;
        
        public NetworkId(uint rawId)
        {
            this.id = rawId;
        }
        
        public NetworkId(int creator, uint index)
        {
            this.id = (uint)creator << 16 | index;
        }
        
        public static NetworkId Invalid => new(0xffffffff);
        
        public bool IsValid() =>Id != 0xffffffff;
        
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
            return $"NetworkId(Created By: {Creator} ID = {Index})";
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
    
    public class NetworkObjectState
    {
        public NetworkId Id = NetworkId.Invalid;

        /// <summary>
        /// The object in the scene associated with this node. This may be null if this node does not represent a scene object.
        /// </summary>
        public NetworkObject AssociatedObject;
        
        /// <summary>
        /// Used to check if incoming ownership transfer requests are valid.
        /// </summary>
        public bool allowOwnershipTransfer = false;

        /// <summary>
        /// All the networked properties of this node. Is null if they have not been set yet.
        /// </summary>
        public List<INetworkProperty> Properties;
        
        /// <summary>
        /// If props have not been set up yet, we cache the data here to be applied later.
        /// </summary>
        public MemoryStream propsData;
        
        /// <summary>
        /// The client with authority over this node. -1 if no one has authority.
        /// </summary>
        public int owner = -1;

        /// <summary>
        /// If this node is still alive in the graph
        /// </summary>
        public bool IsAlive = true;
        
        public static implicit operator bool(NetworkObjectState node) => node?.IsAlive ?? false;

        /// <summary>
        /// Deserialize this node's data from a stream
        /// </summary>
        /// <param name="deserializer"></param>
        public void Deserialize(FoundryDeserializer deserializer)
        {
            if (Properties == null)
            {
                
                deserializer.SetDebugRegion("Cache node Props");
                propsData = deserializer.DeserializeBuffer();
                return;
            }


            deserializer.SetDebugRegion("data size");
            uint propsDataSize = 0;
            deserializer.Deserialize(ref propsDataSize);
            var streamPos = deserializer.stream.Position;
            
            try
            {
                DeserializeProps(deserializer);
                
            }
            catch (Exception e)
            {
                Debug.LogError("Error deserializing properties for node " + Id + ": " + e);
                // Attempt to recover by skipping the rest of the data
                deserializer.stream.Position = streamPos + propsDataSize;
                
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
            uint propsDataSize = 0;
            deserializer.Deserialize(ref propsDataSize);
            var streamPos = deserializer.stream.Position;
            deserializer.stream.Position = streamPos + propsDataSize;
        }
        
        
        /// <summary>
        /// Deserialize this node's properties from a stream
        /// </summary>
        /// <param name="node"></param>
        /// <param name="deserializer"></param>
        public void DeserializeProps(FoundryDeserializer deserializer)
        {
            deserializer.SetDebugRegion("prop count");
            Debug.Assert(Properties != null, "props must be set to deserialize them!");
            uint serializedProps = 0;
            deserializer.Deserialize(ref serializedProps);
            
            while (serializedProps > 0)
            {
                uint propIndex = 0;
                deserializer.SetDebugRegion("prop index");
                deserializer.Deserialize(ref propIndex);
                deserializer.SetDebugRegion("prop data");
                Properties[(int) propIndex].Deserialize(deserializer);
                --serializedProps;
            }
        }

        /// <summary>
        /// Deserialized properties that have been cached on this node.
        /// </summary>
        public void ConsumeCachedProps()
        {
            Debug.Assert(Properties != null, "properties must be set before using cached property data!");
            if (propsData == null)
                return;
            
            FoundryDeserializer deserializer = new(propsData);
            DeserializeProps(deserializer);
            propsData = null;
        }
    }

    public class NetworkState
    {
        /// <summary>
        /// Called when structure events such as adding or parenting nodes have occurred.
        /// </summary>
        public Action<NetworkState> OnStateStructureChanged;

        /// <summary>
        /// Called when a graph delta has been applied.
        /// </summary>
        public Action<NetworkState> OnStateUpdate;
        
        public List<NetworkObjectState> Objects = new();
        public Dictionary<NetworkId, NetworkObjectState> idToNode { get; } = new();


        private INetworkProvider provider;
        
        private int localPlayerId => provider.LocalPlayerId;
        private int graphAuthorityId => provider.GraphAuthorityId;

        /// <summary>
        /// Local counter for id generation
        /// </summary>
        private uint nextId = 0;
        
        private Queue<StructureEvent> structureEvents = new();
        
        public NetworkState(INetworkProvider provider)
        {
            this.provider = provider;
        }
        
        private void RecordEvent(StructureEvent e)
        {
            structureEvents.Enqueue(e);
        }

        public NetworkId NewId()
        {
            //Use the local player id as part of the id to ensure uniqueness across the network, this will work as long as you have less than 65536 players or objects. (Which at that point, you have bigger problems)
            return new NetworkId(localPlayerId, nextId++);
        }

        public NetworkObjectState CreateNode()
        {
            return AddNode(NewId());
        }
        
        public NetworkObjectState AddNode(NetworkId id, bool recordEvent = true)
        {
            
            return AddNode(id, localPlayerId, recordEvent);
        }

        public NetworkObjectState AddNode(NetworkId id, int owner, bool recordEvent = true)
        {
            Debug.Log($"Adding node {id} with owner {owner}");
            // If we receive a node with an id that already exists, this is most likely a duplicate message, so for now we will just ignore it.
            if (idToNode.TryGetValue(id, out var existing))
            {
                Debug.LogWarning("Received creation event duplicate for node with id " + id + ", ignoring.");
                return existing;
            }
            
            // When we receive a node with an id greater than the next id that was created by a previous client with the same id, we need to update our next id to be greater than the received id to avoid id collisions.
            if(id.Creator == localPlayerId && id.Index >= nextId)
                nextId = id.Index + 1;
                
            
            NetworkObjectState node = new NetworkObjectState
            {
                Id = id,
                owner = owner
            };
            
            idToNode[node.Id] = node;
            Objects.Add(node);
            
            if(recordEvent)
                RecordEvent(StructureEvent.Add(id, owner));
            return node;
        }
        
        public void RemoveNode(NetworkId id, bool recordEvent = true)
        {
            Debug.Assert(id.IsValid(), "Cannot remove node with invalid id!");
            var node = idToNode[id];
            
            Objects.Remove(node);
            idToNode.Remove(id);
            node.IsAlive = false;

            if(recordEvent)
                RecordEvent(StructureEvent.Remove(id));
        }
        
        /// <summary>
        /// Change the owner of a node
        /// </summary>
        /// <param name="id"></param>
        /// <param name="newOwner"></param>
        /// <param name="recordEvent"></param>
        public void ChangeOwner(NetworkId id, int newOwner, bool recordEvent = true)
        {
            var node = idToNode[id];
            node.owner = newOwner;
            
            if(recordEvent)
                RecordEvent(StructureEvent.ChangeOwner(id, newOwner));
        }
        
        /// <summary>
        /// Attempt to get a node by its id, returns false if the node does not exist.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool TryGetNode(NetworkId id, out NetworkObjectState node)
        {
            return idToNode.TryGetValue(id, out node);
        }

        /// <summary>
        /// Returns true if the client owns the given node, or is the graph authority.
        /// </summary>
        /// <param name="client">Client to check</param>
        /// <param name="id">Id of the node in question</param>
        /// <returns></returns>
        public bool ClientHasAuthority(int client, NetworkObjectState node)
        {
            return node.owner == client || client == graphAuthorityId;
        }

        /// <summary>
        /// Serialize a node tree, starting at the given node, recursively including all of its children
        /// </summary>
        /// <param name="node">Node to begin at</param>
        /// <param name="serializer"></param>
        /// <param name="serializeAll"></param>
        public void SerializeNode(NetworkObjectState node, FoundrySerializer serializer, bool serializeAll = false)
        {
            serializer.SetDebugRegion("Serialize Node Tree");
            if (node.Properties != null && node.owner == localPlayerId)
            {
                uint dirtyProps = 0;
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
                    var dataSize = serializer.GetPlaceholder<uint>(0);
                    var writeStart = serializer.stream.Position;
                    serializer.SetDebugRegion("prop count");
                    serializer.Serialize(in dirtyProps);
                    uint propIndex = 0;
                    foreach (var prop in node.Properties)
                    {
                        if (prop.Dirty || serializeAll)
                        {
                            serializer.SetDebugRegion("prop index");
                            serializer.Serialize(in propIndex);
                            serializer.SetDebugRegion("prop data");
                            prop.Serialize(serializer, serializeAll);
                            
                            if(!serializeAll)
                                prop.SetClean();
                        }

                        ++propIndex;
                    }

                    serializer.SetDebugRegion("data size");
                    dataSize.WriteValue((uint)(serializer.stream.Position - writeStart));
                }
            }
        }

        /// <summary>
        /// Create a delta containing the full state of all owned nodes in the state.
        /// </summary>
        public byte[] GenerateConstructionDelta()
        {
            MemoryStream stream = new();
            FoundrySerializer serializer = new(stream);

            SerializeStructure(serializer);
            
            foreach (var node in Objects)
                SerializeNode(node, serializer, true);

            serializer.Dispose();
            return stream.ToArray();
        }

        /// <summary>
        /// Generates a serialized delta of the network graph for all reliable properties.
        /// </summary>
        /// <returns>Delta of changed graph properties</returns>
        public byte[] GenerateDelta()
        {
            MemoryStream stream = new();
            FoundrySerializer serializer = new(stream);

            int eventCount = structureEvents.Count;
            if(eventCount > 0)
                Debug.Log("Serializing " + eventCount + " structure events.");
            serializer.Serialize(in eventCount);
            while (structureEvents.Count > 0)
                structureEvents.Dequeue().Serialize(serializer);
            
            foreach (var node in Objects)
                SerializeNode(node, serializer);

            serializer.Dispose();
            return stream.ToArray();
        }

        /// <summary>
        /// Applies the changes recorded in a delta to the network graph.
        /// </summary>
        /// <param name="delta">serialized data</param>
        /// <param name="sender">the client that sent this delta, used for determining if the updates sent are authorized</param>
        /// <param name="clearOnFullGraph">If true, the graph will be cleared and rebuilt if a full graph is received</param>
        /// <returns>Returns true if the graph was applied successfully</returns>
        public void ApplyDelta(byte[] delta, int sender)
        {
            MemoryStream stream = new(delta);
            FoundryDeserializer deserializer = new(stream);

            int structureEventCount = 0;
            deserializer.SetDebugRegion("event count");
            deserializer.Deserialize(ref structureEventCount);

            bool graphChanged = false;
            while (structureEventCount > 0)
            {
                deserializer.SetDebugRegion("Deserialize event");
                StructureEvent structureEvent = new();
                deserializer.Deserialize(ref structureEvent);
                switch (structureEvent.type)
                {
                    case StructureEvent.Type.Add:
                        if (sender == structureEvent.secondaryData || sender == graphAuthorityId)
                            AddNode(structureEvent.id, structureEvent.secondaryData, false);
                        else
                            Debug.LogError("Received add event for node " + structureEvent.id + " but it was ignored.");
                        break;
                    case StructureEvent.Type.Remove:
                    {
                        if (!idToNode.TryGetValue(structureEvent.id, out var node))
                        {
                            Debug.LogWarning("Node " + structureEvent.id + " Was not found!");
                            continue;
                        }
                        if(ClientHasAuthority(sender, node))
                            RemoveNode(structureEvent.id, false);
                        else
                            Debug.LogWarning("Received event for node " + structureEvent.id + " but it was ignored as the sender did not own it.");
                        break;
                    }
                    case StructureEvent.Type.OwnerChange:
                    {
                        bool validEvent = false;
                        if (idToNode.TryGetValue(structureEvent.id, out var node))
                            validEvent =
                                node.AssociatedObject?.VerifyIDChangeRequest(structureEvent.secondaryData) ??
                                node.owner == sender;
                        
                        if(validEvent)
                            ChangeOwner(structureEvent.id, structureEvent.secondaryData, false);
                        else
                            Debug.LogWarning("Received event for node " + structureEvent.id + " but it was ignored as the sender did not own it.");
                        break;
                    }
                    default: // Should never happen
                        Debug.LogError("Deserialized invalid structure event type! " + structureEvent.type);
                        throw new ArgumentOutOfRangeException();
                }
                graphChanged = true;
                --structureEventCount;
            }

            while (stream.Position < stream.Length)
            {
                deserializer.SetDebugRegion("Deserialize node");
                NetworkId nodeId = NetworkId.Invalid;
                deserializer.Deserialize(ref nodeId);
                bool nodeFound = idToNode.TryGetValue(nodeId, out NetworkObjectState node);
                if (!nodeFound)
                {
                    Debug.LogError("Unable to find node with id " + nodeId + " in state! Skipping this node and attempting to recover.");
                    NetworkObjectState.Skip(deserializer);
                    continue;
                }
                if(ClientHasAuthority(sender, node))
                    node.Deserialize(deserializer);
                else
                {
                    NetworkObjectState.Skip(deserializer);
                    Debug.LogError("Received delta for node " + nodeId + " but it was ignored as the sender did not own it.");
                }
            }
            
            deserializer.Dispose();
            
            
            if (graphChanged)
                OnStateStructureChanged?.Invoke(this);
            
            OnStateUpdate?.Invoke(this);
            
        }


        /// <summary>
        /// Serialize the structure of the graph as a sequence of node add events, so that if played back in order, the graph will be reconstructed.
        /// </summary>
        /// <param name="serializer"></param>
        private void SerializeStructure(FoundrySerializer serializer)
        {
            List<StructureEvent> constructionEvents = new(Objects.Count);

            foreach (var node in Objects)
            {
                if(node.owner == localPlayerId)
                    constructionEvents.Add(StructureEvent.Add(node.Id, node.owner));
            }


            int eventCount = constructionEvents.Count;
            serializer.SetDebugRegion("event count");
            serializer.Serialize(in eventCount);

            for (int i = 0; i < eventCount; i++)
            {
                serializer.SetDebugRegion("serialize event");
                var e = constructionEvents[i];
                serializer.Serialize(in e);
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
