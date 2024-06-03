using System.Collections;
using System.Collections.Generic;
using System.IO;
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

        public IFoundrySerializer GetSerializer()
        {
            return new TrackerPosSerializer();
        }
        
        private struct TrackerPosSerializer : IFoundrySerializer
        {
            public void Serialize(in object value, BinaryWriter writer)
            {
                var pos = (TrackerPos)value;
                var v3s = new Vector3Serializer();
                var qs = new QuaternionSerializer();
                v3s.Serialize(pos.translation, writer);
                qs.Serialize(pos.rotation, writer);
                writer.Write(pos.enabled);
            }

            public void Deserialize(ref object value, BinaryReader reader)
            {
                var pos = (TrackerPos)value;
                var v3s = new Vector3Serializer();
                var qs = new QuaternionSerializer();
                object v = pos.translation;
                v3s.Deserialize(ref v, reader);
                pos.translation = (Vector3)v;
                object r = pos.rotation;
                qs.Deserialize(ref r, reader);
                pos.rotation = (Quaternion)r;
                pos.enabled = reader.ReadBoolean();
                value = pos;
            }
        }
    }
}
