using System;
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
        public UInt32 sender;
        public NetworkId id;
        public UInt32 secondaryData;

        public static StructureEvent Add(NetworkId nodeId, UInt32 owner)
        {
            return new StructureEvent
            {
                type = Type.Add,
                sender = UInt32.MaxValue,
                id = nodeId,
                secondaryData = owner
            };
        }

        public static StructureEvent Remove(NetworkId nodeId)
        {
            return new StructureEvent
            {
                type = Type.Remove,
                sender = UInt32.MaxValue,
                id = nodeId,
                secondaryData = UInt32.MaxValue
            };
        }

        public static StructureEvent ChangeOwner(NetworkId nodeId, UInt32 newOwner)
        {
            return new StructureEvent
            {
                type = Type.Remove,
                sender = UInt32.MaxValue,
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
            secondaryData = UInt32.MaxValue;
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
