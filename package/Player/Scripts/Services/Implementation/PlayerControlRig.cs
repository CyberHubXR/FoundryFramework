using System.Collections;
using System.Collections.Generic;
using Foundry.Core.Serialization;
using UnityEngine;

namespace Foundry
{
    public enum TrackingMode
    {
        OnePoint = 0,
        ThreePoint = 1,
        SixPoint = 2
    }

    public enum TrackerType
    {
        head      = 0,
        leftHand  = 1,
        rightHand = 2,
        waist     = 3,
        leftFoot  = 4,
        rightFoot = 5
    }

    public struct TrackerPos: IFoundrySerializable
    {
        public Vector3 translation;
        public Quaternion rotation;
        public bool enabled;
        
        public void Serialize(FoundrySerializer serializer)
        {
            serializer.Serialize(in translation);
            serializer.Serialize(in rotation);
            serializer.Serialize(in enabled);
        }

        public void Deserialize(FoundryDeserializer deserializer)
        {
            deserializer.Deserialize(ref translation);
            deserializer.Deserialize(ref rotation);
            deserializer.Deserialize(ref enabled);
        }
    }
}
