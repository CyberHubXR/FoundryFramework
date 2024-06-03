using System.IO;
using UnityEngine;

namespace Foundry.Core.Serialization
{
    public struct Vector2Serializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            var v = (Vector2)value;
            writer.Write(v.x);
            writer.Write(v.y);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            value = new Vector2(x, y);
        }
    }
    
    public struct Vector2IntSerializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            var v = (Vector2Int)value;
            writer.Write(v.x);
            writer.Write(v.y);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            var x = reader.ReadInt32();
            var y = reader.ReadInt32();
            value = new Vector2Int(x, y);
        }
    }
    
    public struct Vector3Serializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            var v = (Vector3)value;
            writer.Write(v.x);
            writer.Write(v.y);
            writer.Write(v.z);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            value = new Vector3(x, y, z);
        }
    }
    
    public struct Vector3IntSerializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            var v = (Vector3Int)value;
            writer.Write(v.x);
            writer.Write(v.y);
            writer.Write(v.z);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            var x = reader.ReadInt32();
            var y = reader.ReadInt32();
            var z = reader.ReadInt32();
            value = new Vector3Int(x, y, z);
        }
    }
    
    public struct Vector4Serializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            var v = (Vector4)value;
            writer.Write(v.x);
            writer.Write(v.y);
            writer.Write(v.z);
            writer.Write(v.w);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            var w = reader.ReadSingle();
            value = new Vector4(x, y, z, w);
        }
    }
    
    public struct QuaternionSerializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            var q = (Quaternion)value;
            writer.Write(q.x);
            writer.Write(q.y);
            writer.Write(q.z);
            writer.Write(q.w);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            var w = reader.ReadSingle();
            value = new Quaternion(x, y, z, w);
        }
    }
    
    public struct ColorSerializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            var c = (Color)value;
            writer.Write(c.r);
            writer.Write(c.g);
            writer.Write(c.b);
            writer.Write(c.a);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            var r = reader.ReadSingle();
            var g = reader.ReadSingle();
            var b = reader.ReadSingle();
            var a = reader.ReadSingle();
            value = new Color(r, g, b, a);
        }
    }
    
    public struct Color32Serializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            var c = (Color32)value;
            writer.Write(c.r);
            writer.Write(c.g);
            writer.Write(c.b);
            writer.Write(c.a);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            var r = reader.ReadByte();
            var g = reader.ReadByte();
            var b = reader.ReadByte();
            var a = reader.ReadByte();
            value = new Color32(r, g, b, a);
        }
    }
    
    public struct Matrix4x4Serializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            var m = (Matrix4x4)value;
            for (int i = 0; i < 16; i++)
                writer.Write(m[i]);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            var m = new Matrix4x4();
            for (int i = 0; i < 16; i++)
                m[i] = reader.ReadSingle();
            value = m;
        }
    }
}