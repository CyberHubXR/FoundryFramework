using System;
using System.IO;

namespace Foundry.Core.Serialization
{
    public struct BoolSerializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            writer.Write((bool)value);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            value = reader.ReadBoolean();
        }
    }
    
    public struct ByteSerializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            writer.Write((byte)value);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            value = reader.ReadByte();
        }
    }
    
    public struct SByteSerializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            writer.Write((sbyte)value);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            value = reader.ReadSByte();
        }
    }   
    
    public struct CharSerializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            writer.Write((char)value);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            value = reader.ReadChar();
        }
    }
    
    public struct ShortSerializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            writer.Write((short)value);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            value = reader.ReadInt16();
        }
    }
    
    public struct UShortSerializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            writer.Write((ushort)value);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            value = reader.ReadUInt16();
        }
    }
    
    public struct IntSerializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            writer.Write((int)value);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            value = reader.ReadInt32();
        }
    }
    
    public struct UIntSerializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            writer.Write((uint)value);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            value = reader.ReadUInt32();
        }
    }
    
    public struct LongSerializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            writer.Write((long)value);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            value = reader.ReadInt64();
        }
    }
    
    public struct ULongSerializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            writer.Write((ulong)value);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            value = reader.ReadUInt64();
        }
    }
    
    public struct FloatSerializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            writer.Write((float)value);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            value = reader.ReadSingle();
        }
    }
    
    public struct DecimalSerializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            writer.Write((decimal)value);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            value = reader.ReadDecimal();
        }
    }
    
    public struct DoubleSerializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            writer.Write((double)value);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            value = reader.ReadDouble();
        }
    }
    
    public struct StringSerializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            var str = (string)value;
            writer.Write((UInt64)str.Length);
            for (int i = 0; i < str.Length; i++)
                writer.Write((byte)str[i]);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            var length = (int)reader.ReadUInt64();
            var str = new char[length];
            for (int i = 0; i < length; i++)
                str[i] = (char)reader.ReadByte();
            value = new string(str);
        }
    }
    
    public struct ByteArraySerializer : IFoundrySerializer
    {
        public void Serialize(in object value, BinaryWriter writer)
        {
            writer.Write((byte[])value);
        }

        public void Deserialize(ref object value, BinaryReader reader)
        {
            value = reader.ReadBytes(reader.ReadInt32());
        }
    }
}