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
        /// Write contained data out to the stream to be synced across the network.
        /// </summary>
        /// <param name="serializer"></param>
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

    [Serializable]
    public struct NetworkId : IFoundrySerializable
    {
        /// <summary>
        /// The player that owns this object
        /// </summary>
        public int Owner => owner;
        private int owner;

        /// <summary>
        /// Locally unique id of this object
        /// </summary>
        public uint ID => id;
        private uint id;
        
        public NetworkId(int owner, uint id)
        {
            this.owner = owner;
            this.id = id;
        }
        
        public static NetworkId Invalid => new(-1, 0xffffffff);
        
        public bool IsValid() => Owner != -1 && ID != 0xffffffff;
        
        /// <summary>
        /// Override of hashing function to allow for use in dictionaries
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(owner, id);
        }

        public override string ToString()
        {
            return "NetworkId(Owner = " + Owner + ", ID = " + ID + ")";
        }

        public void Serialize(FoundrySerializer serializer)
        {
            serializer.Serialize(in owner);
            serializer.Serialize(in id);
        }

        public void Deserialize(FoundryDeserializer deserializer)
        {
            deserializer.Deserialize(ref owner);
            deserializer.Deserialize(ref id);
        }

        public static bool operator ==(NetworkId a, NetworkId b)
        {
            return a.owner == b.owner && a.id == b.id;
        }
        
        public static bool operator !=(NetworkId a, NetworkId b)
        {
            return a.owner != b.owner || a.id != b.id;
        }
    }
    
    public class NetworkGraphNode
    {
        public NetworkId ID = NetworkId.Invalid;

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
        
        public NetworkGraphNode Parent;
        public List<NetworkGraphNode> Children = new();

        /// <summary>
        /// If this node is still alive in the graph
        /// </summary>
        public bool IsAlive = true;
        
        public static implicit operator bool(NetworkGraphNode node) => node?.IsAlive ?? false;

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
                Debug.LogError("Error deserializing properties for node " + ID + ": " + e);
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

    public class NetworkGraph
    {
        /// <summary>
        /// Called when the graph has been rebuilt and references to nodes may have been broken.
        /// </summary>
        public Action<NetworkGraph> OnGraphRebuilt;

        /// <summary>
        /// Called when structure events such as adding or parenting nodes have occurred.
        /// </summary>
        public Action<NetworkGraph> OnGraphChanged;

        /// <summary>
        /// Called when a graph delta has been applied.
        /// </summary>
        public Action<NetworkGraph> OnGraphUpdate;
        
        public List<NetworkGraphNode> RootNodes = new();
        
        /// <summary>
        /// Callback set by the network provider to get the current graph authority.
        /// </summary>
        public GetIDDelegate GetMasterID;

        public GetIDDelegate GetLocalPlayerID;
        public delegate int GetIDDelegate();

        private int localPlayerId => GetLocalPlayerID();
        private uint nextId = 0;
        public Dictionary<NetworkId, NetworkGraphNode> idToNode { get; } = new();

        private struct StructureEvent : IFoundrySerializable
        {
            public enum Type : uint
            { 
                /// <summary>
                /// Event when a node is added to the graph
                /// </summary>
                Add = 0, 
                /// <summary>
                /// Event when a node is removed from the graph
                /// </summary>
                Remove = 1,
                /// <summary>
                /// Event when a has changed parents
                /// </summary>
                Parent = 2,
                /// <summary>
                /// Event when a node's Id has changed, as the owner is part of the id, this may represent a change in ownership
                /// Id changes are verified on a per case basis, as we may sometimes want to allow id changes from clients without ownership.
                /// </summary>
                ChangeId = 3
            }
            
            public Type type;
            public int sender;
            public NetworkId id;
            public NetworkId secondaryId;

            public static StructureEvent Add(NetworkId nodeId)
            {
                return new StructureEvent
                {
                    type = Type.Add,
                    sender = -1,
                    id = nodeId,
                    secondaryId = NetworkId.Invalid
                };
            }

            public static StructureEvent Add(NetworkId nodeId, NetworkId parentId)
            {
                return new StructureEvent
                {
                    type = Type.Add,
                    sender = -1,
                    id = nodeId,
                    secondaryId = parentId
                };
            }
            
            public static StructureEvent Remove(NetworkId nodeId)
            {
                return new StructureEvent
                {
                    type = Type.Remove,
                    sender = -1,
                    id = nodeId,
                    secondaryId = NetworkId.Invalid
                };
            }
            
            public static StructureEvent Parent(NetworkId nodeId, NetworkId parentId)
            {
                return new StructureEvent
                {
                    type = Type.Parent,
                    sender = -1,
                    id = nodeId,
                    secondaryId = parentId
                };
            }

            public static StructureEvent ChangeId(NetworkId oldId, NetworkId newId)
            {
                return new StructureEvent
                {
                    type = Type.ChangeId,
                    sender = -1,
                    id = oldId,
                    secondaryId = newId
                };
            }
            
            public void Serialize(FoundrySerializer serializer)
            {
                serializer.SetDebugRegion("StructureEvent");
                uint typeIndex = (uint)type;
                serializer.Serialize(in typeIndex);
                serializer.Serialize(in id);
                switch (type)
                {
                    case Type.Add:
                        serializer.Serialize(in secondaryId);
                        break;
                    case Type.Remove:
                        break;
                    case Type.Parent:
                        serializer.Serialize(in secondaryId);
                        break;
                    case Type.ChangeId:
                        serializer.Serialize(in secondaryId);
                        break;
                }

            }

            public void Deserialize(FoundryDeserializer deserializer)
            {
                deserializer.SetDebugRegion("StructureEvent");
                uint typeIndex = default;
                deserializer.Deserialize(ref typeIndex);
                type = (Type)typeIndex;
                deserializer.Deserialize(ref id);
                secondaryId = NetworkId.Invalid;
                switch (type)
                {
                    case Type.Add:
                        deserializer.Deserialize(ref secondaryId);
                        break;
                    case Type.Remove:
                        secondaryId = NetworkId.Invalid;
                        break;
                    case Type.Parent:
                        deserializer.Deserialize(ref secondaryId);
                        break;
                    case Type.ChangeId:
                        deserializer.Deserialize(ref secondaryId);
                        break;
                }
            }
        }
        
        private Queue<StructureEvent> structureEvents = new();
        
        private void RecordEvent(StructureEvent e)
        {
            structureEvents.Enqueue(e);
        }

        public NetworkId NewId(int owner)
        {
            return new NetworkId(owner, nextId++);
        }

        public NetworkGraphNode CreateNode()
        {
            return CreateNode(NetworkId.Invalid);
        }

        public NetworkGraphNode CreateNode(NetworkId parentId)
        {
            return AddNode(NewId(localPlayerId), parentId);
        }

        public NetworkGraphNode AddNode(NetworkId id, NetworkId parentId, bool recordEvent = true)
        {
            // If we receive a node with an id that already exists, this is most likely a duplicate message, so for now we will just ignore it.
            if (idToNode.TryGetValue(id, out var existing))
                return existing;
            
            NetworkGraphNode node = new NetworkGraphNode
            {
                ID = id
            };
            
            idToNode[node.ID] = node;
            if (!parentId.IsValid())
                RootNodes.Add(node);
            else
            {
                node.Parent = idToNode[parentId];
                node.Parent.Children.Add(node);
            }
            if(recordEvent)
                RecordEvent(StructureEvent.Add(id, parentId));
            return node;
        }
        
        public void RemoveNode(NetworkId id, bool recordEvent = true)
        {
            var node = idToNode[id];
            
            while(node.Children.Count > 0)
                RemoveNode(node.Children[0].ID, false);
            
            if (node.Parent != null)
                node.Parent.Children.Remove(node);
            else
                RootNodes.Remove(node);
            idToNode.Remove(id);
            node.IsAlive = false;

            if(recordEvent)
                RecordEvent(StructureEvent.Remove(id));
        }
        
        public void SetNodeParent(NetworkId id, NetworkId parentId, bool recordEvent = true)
        {
            var node = idToNode[id];
            var newParent = parentId.IsValid() ? idToNode[parentId] : null;
            if (node.Parent == newParent)
                return;
            
            if (node.Parent != null)
                node.Parent.Children.Remove(node);
            else
                RootNodes.Remove(node);
            
            node.Parent = newParent;
            if(newParent != null)
                node.Parent.Children.Add(node);
            else
                RootNodes.Add(node);
            if(recordEvent)
                RecordEvent(StructureEvent.Parent(id, parentId));
        }
        
        public void ChangeId(NetworkId oldId, NetworkId newId, bool recordEvent = true)
        {
            var node = idToNode[oldId];
            node.ID = newId;
            idToNode.Remove(oldId);
            idToNode.Add(newId, node);

            if(node.AssociatedObject)
                node.AssociatedObject.NetworkedGraphId.Value = newId;
            
            if(recordEvent)
                RecordEvent(StructureEvent.ChangeId(oldId, newId));
        }
        
        /// <summary>
        /// Attempt to get a node by its id, returns false if the node does not exist.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool TryGetNode(NetworkId id, out NetworkGraphNode node)
        {
            return idToNode.TryGetValue(id, out node);
        }

        /// <summary>
        /// Returns true if the client owns the given node, or is the graph authority.
        /// </summary>
        /// <param name="client">Client to check</param>
        /// <param name="id">Id of the node in question</param>
        /// <returns></returns>
        public bool ClientHasAuthority(int client, NetworkId id)
        {
            return client == id.Owner || client == GetMasterID();
        }

        /// <summary>
        /// Serialize a node tree, starting at the given node, recursively including all of its children
        /// </summary>
        /// <param name="node">Node to begin at</param>
        /// <param name="serializer"></param>
        /// <param name="serializeAll"></param>
        public void SerializeNodeTree(NetworkGraphNode node, FoundrySerializer serializer, bool serializeAll = false)
        {
            serializer.SetDebugRegion("Serialize Node Tree");
            if (node.Properties != null && (node.ID.Owner == localPlayerId || serializeAll))
            {
                uint dirtyProps = 0;
                foreach (var prop in node.Properties)
                {
                    if (serializeAll)
                        prop.SetDirty();
                    if (prop.Dirty)
                        ++dirtyProps;
                }

                if (dirtyProps > 0 || serializeAll)
                {
                    serializer.SetDebugRegion("node id");
                    serializer.Serialize(in node.ID);
                    serializer.SetDebugRegion("data size");
                    var dataSize = serializer.GetPlaceholder<uint>(0);
                    var writeStart = serializer.stream.Position;
                    serializer.SetDebugRegion("prop count");
                    serializer.Serialize(in dirtyProps);
                    uint propIndex = 0;
                    foreach (var prop in node.Properties)
                    {
                        if (prop.Dirty)
                        {
                            serializer.SetDebugRegion("prop index");
                            serializer.Serialize(in propIndex);
                            serializer.SetDebugRegion("prop data");
                            prop.Serialize(serializer);
                            
                            // Possible this will cause a lag spike as everyone is sent the full graph possibly twice,
                            // but when a new person joins we send only them a new graph and that would clear all dirty flags,
                            // so it's better to send the full graph than miss a few updates. Definitely will revisit this later.
                            if(!serializeAll)
                                prop.SetClean();
                        }

                        ++propIndex;
                    }

                    serializer.SetDebugRegion("data size");
                    dataSize.WriteValue((uint)(serializer.stream.Position - writeStart));
                }
            }

            foreach (var child in node.Children)
                SerializeNodeTree(child, serializer, serializeAll);
        }

        /// <summary>
        /// Generates a serialized delta of the network graph for all reliable properties.
        /// </summary>
        /// <returns>Delta of changed graph properties</returns>
        public NetworkGraphDelta GenerateDelta()
        {
            MemoryStream stream = new();
            FoundrySerializer serializer = new(stream);
            
            // Serialize "false" to indicate that this is a delta and not a full graph
            bool isFullGraph = false;
            serializer.Serialize(in isFullGraph);

            int eventCount = structureEvents.Count;
            serializer.Serialize(in eventCount);
            while (structureEvents.Count > 0)
                structureEvents.Dequeue().Serialize(serializer);
            
            foreach (var node in RootNodes)
                SerializeNodeTree(node, serializer);

            serializer.Dispose();
            return new NetworkGraphDelta()
            {
                data = stream.ToArray()
            };
        }

        /// <summary>
        /// Applies the changes recorded in a delta to the network graph.
        /// </summary>
        /// <param name="delta">serialized data</param>
        /// <param name="sender">the client that sent this delta, used for determining if the updates sent are authorized</param>
        /// <param name="clearOnFullGraph">If true, the graph will be cleared and rebuilt if a full graph is received</param>
        /// <returns>Returns true if the graph was applied successfully</returns>
        public void ApplyDelta(ref NetworkGraphDelta delta, int sender, bool clearOnFullGraph = true)
        {
            MemoryStream stream = new(delta.data);
            FoundryDeserializer deserializer = new(stream);

            bool isFullGraph = false;
            deserializer.Deserialize(ref isFullGraph);
            if (isFullGraph && clearOnFullGraph)
            {
                foreach(var node in idToNode.Values)
                    node.IsAlive = false;
                RootNodes.Clear();
                idToNode.Clear();
                
                deserializer.StartDebugDump();
            }

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
                        if(ClientHasAuthority(sender, structureEvent.id))
                            AddNode(structureEvent.id, structureEvent.secondaryId, false);
                        else
                            Debug.LogWarning("Received event for node " + structureEvent.id + " but it was ignored as the sender did not own it.");
                        break;
                    case StructureEvent.Type.Remove:
                        if(ClientHasAuthority(sender, structureEvent.id))
                            RemoveNode(structureEvent.id, false);
                        else
                            Debug.LogWarning("Received event for node " + structureEvent.id + " but it was ignored as the sender did not own it.");
                        break;
                    case StructureEvent.Type.Parent:
                        if(ClientHasAuthority(sender, structureEvent.id))
                            SetNodeParent(structureEvent.id, structureEvent.secondaryId, false);
                        else
                            Debug.LogWarning("Received event for node " + structureEvent.id + " but it was ignored as the sender did not own it.");
                        break;
                    case StructureEvent.Type.ChangeId:
                        bool validEvent = ClientHasAuthority(sender, structureEvent.id);
                        if (!validEvent && idToNode.TryGetValue(structureEvent.id, out var node))
                            validEvent = node.AssociatedObject?.VerifyIDChangeRequest(sender, structureEvent.secondaryId) ?? false;
                        Debug.Log($"Changed node {structureEvent.id} to {structureEvent.secondaryId} with validEvent {validEvent}");
                        
                        if(validEvent)
                            ChangeId(structureEvent.id, structureEvent.secondaryId, false);
                        else
                            Debug.LogWarning("Received event for node " + structureEvent.id + " but it was ignored as the sender did not own it.");
                        break;
                    default: // Should never happen
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
                bool nodeFound = idToNode.TryGetValue(nodeId, out NetworkGraphNode node);
                if (!nodeFound)
                {
                    Debug.LogError("Unable to find node with id " + nodeId + " in graph! Skipping this node and attempting to recover.");
                    NetworkGraphNode.Skip(deserializer);
                    continue;
                }
                if(ClientHasAuthority(sender, nodeId))
                    node.Deserialize(deserializer);
                else
                {
                    NetworkGraphNode.Skip(deserializer);
                    Debug.LogError("Received delta for node " + nodeId + " but it was ignored as the sender did not own it.");
                }
            }
            
            deserializer.Dispose();
            
            
            if (graphChanged)
                OnGraphChanged?.Invoke(this);
            
            if(isFullGraph)
                OnGraphRebuilt?.Invoke(this);
            OnGraphUpdate?.Invoke(this);
            
        }


        /// <summary>
        /// Serialize the structure of the graph as a sequence of node add events, so that if played back in order, the graph will be reconstructed.
        /// </summary>
        /// <param name="serializer"></param>
        private void SerializeStructure(FoundrySerializer serializer)
        {
            List<StructureEvent> constructionEvents = new();
            
            foreach (var node in RootNodes)
                GenerateConstructionEvents(constructionEvents, node);


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
        
        private void GenerateConstructionEvents(List<StructureEvent> events, NetworkGraphNode node)
        {
            events.Add(StructureEvent.Add(node.ID, node.Parent?.ID ?? NetworkId.Invalid));
            foreach (var child in node.Children)
                GenerateConstructionEvents(events, child);
        }

        /// <summary>
        /// Serialize the full graph, including construction events.
        /// </summary>
        /// <returns></returns>
        public NetworkGraphDelta SerializeFull()
        {
            MemoryStream stream = new();
            FoundrySerializer serializer = new(stream);
            
            // Serialize "true" to indicate that this is a full graph
            bool isFullGraph = true;
            serializer.Serialize(in isFullGraph);
            serializer.StartDebugDump();

            SerializeStructure(serializer);

            foreach (var node in RootNodes)
                SerializeNodeTree(node, serializer, true);

            serializer.Dispose();
            return new NetworkGraphDelta()
            {
                data = stream.ToArray()
            };
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
