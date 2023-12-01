using System.Collections;
using System.Collections.Generic;
using Foundry.Core.Serialization;
using UnityEngine;

namespace Foundry.Networking
{
    class StructureEvent : IFoundrySerializable
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
            /// Event when a node changes it's owner
            /// </summary>
            OwnerChange = 2
        }

        public Type type;
        public int sender;
        public NetworkId id;
        public int secondaryData;

        public static StructureEvent Add(NetworkId nodeId, int owner)
        {
            return new StructureEvent
            {
                type = Type.Add,
                sender = -1,
                id = nodeId,
                secondaryData = owner
            };
        }

        public static StructureEvent Remove(NetworkId nodeId)
        {
            return new StructureEvent
            {
                type = Type.Remove,
                sender = -1,
                id = nodeId,
                secondaryData = -1
            };
        }

        public static StructureEvent ChangeOwner(NetworkId nodeId, int newOwner)
        {
            return new StructureEvent
            {
                type = Type.Remove,
                sender = -1,
                id = nodeId,
                secondaryData = newOwner
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
                    serializer.Serialize(in secondaryData);
                    break;
                case Type.OwnerChange:
                    serializer.Serialize(in secondaryData);
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
            secondaryData = -1;
            switch (type)
            {
                case Type.Add:
                    deserializer.Deserialize(ref secondaryData);
                    break;
                case Type.OwnerChange:
                    deserializer.Deserialize(ref secondaryData);
                    break;
            }
        }
    }
}
