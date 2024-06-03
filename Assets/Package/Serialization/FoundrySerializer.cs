using System;
using System.IO;
using UnityEngine;

namespace Foundry.Core.Serialization
{
    public interface IFoundrySerializable
    {
        IFoundrySerializer GetSerializer();
    }

    public interface IFoundrySerializer
    {
        void Serialize(in object value, BinaryWriter writer);

        void Deserialize(ref object value, BinaryReader reader);
    }
    
    public static class FoundrySerializerFinder
    {
        public static IFoundrySerializer GetSerializer(Type type)
        {
            if (type == typeof(bool))
                return new BoolSerializer();
            if (type == typeof(byte))
                return new ByteSerializer();
            if (type == typeof(sbyte))
                return new SByteSerializer();
            if (type == typeof(char))
                return new CharSerializer();
            if (type == typeof(short))
                return new ShortSerializer();
            if (type == typeof(ushort))
                return new UShortSerializer();
            if (type == typeof(int))
                return new IntSerializer();
            if (type == typeof(uint))
                return new UIntSerializer();
            if (type == typeof(float))
                return new FloatSerializer();
            if (type == typeof(decimal))
                return new DecimalSerializer();
            if (type == typeof(double))
                return new DoubleSerializer();
            if (type == typeof(long))
                return new LongSerializer();
            if (type == typeof(ulong))
                return new ULongSerializer();
            if (type == typeof(string))
                return new StringSerializer();
            if (type == typeof(Vector2))
                return new Vector2Serializer();
            if (type == typeof(Vector3))
                return new Vector3Serializer();
            if (type == typeof(Vector4))
                return new Vector4Serializer();
            if (type == typeof(Quaternion))
                return new QuaternionSerializer();
            if (type == typeof(Matrix4x4))
                return new Matrix4x4Serializer();
            if (type == typeof(Vector2Int))
                return new Vector2IntSerializer();
            if (type == typeof(Vector3Int))
                return new Vector3IntSerializer();
            if (type == typeof(Color))
                return new ColorSerializer();
            if (type == typeof(Color32))
                return new Color32Serializer();
            if (type.IsEnum)
                return new IntSerializer();
            throw new NotSupportedException($"No serializer found for type {type}");
        }
    }

    public class UInt32Placehodler
    {
        private BinaryWriter writer;
        private long streamPos;
        public UInt32Placehodler(BinaryWriter writer)
        {
            this.writer = writer;
            streamPos = writer.BaseStream.Position;
            writer.BaseStream.Seek(4, SeekOrigin.Current);
        }
        
        public void WriteValue(uint value)
        {
            long currentStreamPos = writer.BaseStream.Position;
            writer.BaseStream.Position = streamPos;
            writer.Write(value);
            writer.BaseStream.Position = currentStreamPos;
        }
    }
    
    public class UInt64Placehodler
    {
        private BinaryWriter writer;
        private long streamPos;
        public UInt64Placehodler(BinaryWriter writer)
        {
            this.writer = writer;
            streamPos = writer.BaseStream.Position;
            writer.BaseStream.Seek(8, SeekOrigin.Current);
        }
        
        public void WriteValue(ulong value)
        {
            long currentStreamPos = writer.BaseStream.Position;
            writer.BaseStream.Position = streamPos;
            writer.Write(value);
            writer.BaseStream.Position = currentStreamPos;
        }
    }

    /*/// <summary>
    /// Struct for reserving a spot in a stream for data that will be written later.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct FoundrySerializedDataPlaceholder<T>
        where T : unmanaged
    {
        public long streamPos { get; private set; }
        
        private FoundrySerializer serializer;


        public FoundrySerializedDataPlaceholder(FoundrySerializer serializer, T placeholder)
        {
            this.serializer = serializer;
            streamPos = this.serializer.stream.Position;
            
            #if DEVELOPMENT_BUILD || UNITY_EDITOR
            string dumpWriterCache = this.serializer.debugRegionName;
            this.serializer.debugRegionName = $"Placeholder({dumpWriterCache})";
            #endif
            
            serializer.Serialize(in placeholder);
            
            #if DEVELOPMENT_BUILD || UNITY_EDITOR
            this.serializer.debugRegionName = dumpWriterCache;
            #endif
        }

        /// <summary>
        /// Moves the stream position to the placeholder and writes the value, before moving the stream position back to where it was.
        /// </summary>
        /// <param name="value"></param>
        public void WriteValue(T value)
        {
            long currentStreamPos = serializer.stream.Position;
            serializer.stream.Position = streamPos;
            
            #if DEVELOPMENT_BUILD || UNITY_EDITOR
            var debugDumpWriterCache = serializer.debugDumpWriter;
            serializer.debugDumpWriter = null;
            #endif
            
            serializer.Serialize(in value);
            
            #if DEVELOPMENT_BUILD || UNITY_EDITOR
            serializer.debugDumpWriter = debugDumpWriterCache;
            #endif
            
            serializer.stream.Position = currentStreamPos;
        }
    }
    
    public class FoundrySerializer : IDisposable
    {
        public Stream stream { get; private set; }
        BinaryWriter writer;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private static int _debugDumpCount;
        public StreamWriter debugDumpWriter;
        public string debugRegionName = "";
#endif

        public FoundrySerializer(Stream stream, bool debugDump = false)
        {
            this.stream = stream;
            writer = new BinaryWriter(stream, new UTF8Encoding());
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if(debugDump)
                StartDebugDump();
#endif
        }
        
        public void StartDebugDump()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            System.IO.Directory.CreateDirectory("Temp");
            debugDumpWriter = new StreamWriter($"Temp/FoundrySerializerDebugDump{_debugDumpCount}.csv");
            _debugDumpCount += 1;
            debugDumpWriter.WriteLine($"position\tvalue\ttype\tdata size\tregion");
#endif
        }


        public void SetDebugRegion(string name)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (debugDumpWriter == null)
                return;
            debugRegionName = name;
#endif
        }
        
        public void Dispose()
        {
            writer.Dispose();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (debugDumpWriter != null)
            {
                Debug.Log($"Debug dump written to {Path.GetFullPath("Temp/")}");
                debugDumpWriter.Dispose();
            }
#endif
        }

        /// <summary>
        /// Use this when you want to serialize data at a position, but don't have the data yet.
        /// For example, serializing the size of a dynamically sized message before the message itself.
        /// This only works for statically sized objects such as int, and not strings.
        /// </summary>
        /// <typeparam name="T">The object type to serialize</typeparam>
        public FoundrySerializedDataPlaceholder<T> GetPlaceholder<T>(T placeholder)
            where T : unmanaged
        {
            return new FoundrySerializedDataPlaceholder<T>(this, placeholder);
        }
        
        public void Serialize<T>(in T value)
            where T : notnull
        {
            #if DEVELOPMENT_BUILD || UNITY_EDITOR
            var start = stream.Position;
            #endif
            
            // Native C# types
            if (typeof(T) == typeof(bool))
                writer.Write((bool)(object)value);
            else if (typeof(T) == typeof(byte))
                writer.Write((byte)(object)value);
            else if (typeof(T) == typeof(byte[]))
            {
                var bytes = (byte[])(object)value;
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
            else if (typeof(T) == typeof(sbyte))
                writer.Write((sbyte)(object)value);
            else if (typeof(T) == typeof(char))
                writer.Write((char)(object)value);
            else if (typeof(T) == typeof(short))
                writer.Write((short)(object)value);
            else if (typeof(T) == typeof(ushort))
                writer.Write((ushort)(object)value);
            else if (typeof(T) == typeof(int))
                writer.Write((int)(object)value);
            else if (typeof(T) == typeof(uint))
                writer.Write((uint)(object)value);
            else if (typeof(T) == typeof(float))
                writer.Write((float)(object)value);
            else if (typeof(T) == typeof(decimal))
                writer.Write((decimal)(object)value);
            else if (typeof(T) == typeof(double))
                writer.Write((double)(object)value);
            else if (typeof(T) == typeof(long))
                writer.Write((long)(object)value);
            else if (typeof(T) == typeof(ulong))
                writer.Write((ulong)(object)value);
            else if (typeof(T) == typeof(string))
            {
                var str = ((string)(object)value);
                UInt64 length = (UInt64)str.Length;
                writer.Write(length);
                for (int i = 0; i < str.Length; i++)
                    writer.Write((byte)str[i]);
            }
            else if (typeof(T).IsEnum)
                writer.Write((int)(object)value);
            
            // Unity types
            else if (typeof(T) == typeof(Vector2))
            {
                Vector2 vector = (Vector2)(object)value;
                writer.Write(vector.x);
                writer.Write(vector.y);
            }
            else if (typeof(T) == typeof(Vector3))
            {
                Vector3 vector = (Vector3)(object)value;
                writer.Write(vector.x);
                writer.Write(vector.y);
                writer.Write(vector.z);
            }
            else if (typeof(T) == typeof(Vector4))
            {
                Vector4 vector = (Vector4)(object)value;
                writer.Write(vector.x);
                writer.Write(vector.y);
                writer.Write(vector.z);
                writer.Write(vector.w);
            }
            else if (typeof(T) == typeof(Quaternion))
            {
                Quaternion quaternion = (Quaternion)(object)value;
                writer.Write(quaternion.x);
                writer.Write(quaternion.y);
                writer.Write(quaternion.z);
                writer.Write(quaternion.w);
            }
            else if (typeof(T) == typeof(Matrix4x4))
            {
                Matrix4x4 matrix = (Matrix4x4)(object)value;
                for (int i = 0; i < 16; i++)
                    writer.Write(matrix[i]);
            }
            else if (typeof(T) == typeof(Vector2Int))
            {
                Vector2Int vector = (Vector2Int)(object)value;
                writer.Write(vector.x);
                writer.Write(vector.y);
            }
            else if (typeof(T) == typeof(Vector3Int))
            {
                Vector3Int vector = (Vector3Int)(object)value;
                writer.Write(vector.x);
                writer.Write(vector.y);
                writer.Write(vector.z);
            }
            else if (typeof(T) == typeof(BoundingSphere))
            {
                BoundingSphere sphere = (BoundingSphere)(object)value;
                writer.Write(sphere.position.x);
                writer.Write(sphere.position.y);
                writer.Write(sphere.position.z);
                writer.Write(sphere.radius);
            }
            else if (typeof(T) == typeof(Bounds))
            {
                Bounds bounds = (Bounds)(object)value;
                writer.Write(bounds.center.x);
                writer.Write(bounds.center.y);
                writer.Write(bounds.center.z);
                writer.Write(bounds.size.x);
                writer.Write(bounds.size.y);
                writer.Write(bounds.size.z);
            }
            else if (typeof(T) == typeof(Rect))
            {
                Rect rect = (Rect)(object)value;
                writer.Write(rect.x);
                writer.Write(rect.y);
                writer.Write(rect.width);
                writer.Write(rect.height);
            }
            else if (typeof(T) == typeof(BoundsInt))
            {
                BoundsInt bounds = (BoundsInt)(object)value;
                writer.Write(bounds.position.x);
                writer.Write(bounds.position.y);
                writer.Write(bounds.position.z);
                writer.Write(bounds.size.x);
                writer.Write(bounds.size.y);
                writer.Write(bounds.size.z);
            }
            else if (typeof(T) == typeof(RectInt))
            {
                RectInt rect = (RectInt)(object)value;
                writer.Write(rect.x);
                writer.Write(rect.y);
                writer.Write(rect.width);
                writer.Write(rect.height);
            }
            else if (typeof(T) == typeof(Color))
            {
                Color color = (Color)(object)value;
                writer.Write(color.r);
                writer.Write(color.g);
                writer.Write(color.b);
                writer.Write(color.a);
            }
            else if (typeof(T) == typeof(Color32))
            {
                Color32 color = (Color32)(object)value;
                writer.Write(color.r);
                writer.Write(color.g);
                writer.Write(color.b);
                writer.Write(color.a);
            }

                // Foundry types
            else if (typeof(IFoundrySerializable).IsAssignableFrom(typeof(T)))
            {
                ((IFoundrySerializable)value).Serialize(this);
            }
            else
                throw new NotSupportedException($"Serialization for type {typeof(T)} is not supported. Please implement IFoundrySerializable if you are attempting to serialize a custom type.");
            
            #if DEVELOPMENT_BUILD || UNITY_EDITOR
            if(debugDumpWriter != null)
                debugDumpWriter.WriteLine($"{start}\t{value.ToString()}\t{typeof(T).Name}\t{stream.Position - start}\t{debugRegionName}");
            #endif
        }
    }
    
    public class FoundryDeserializer : IDisposable
    {
        public Stream stream { get; private set; }
        private BinaryReader reader;
        
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        static int _debugDumpCount;
        private StreamWriter debugDumpWriter;
        private string debugRegionName = "";
#endif
        
        public FoundryDeserializer(Stream stream, bool debugDump = false)
        {
            this.stream = stream;
            reader = new BinaryReader(stream, new UTF8Encoding());
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if(debugDump)
                StartDebugDump();
#endif
        }

        public void StartDebugDump()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            System.IO.Directory.CreateDirectory("Temp");
            debugDumpWriter = new StreamWriter($"Temp/FoundryDeserializerDebugDump{_debugDumpCount}.csv");
            _debugDumpCount += 1;
            debugDumpWriter.WriteLine($"position\tvalue\ttype\tdata size\tregion");
#endif
        }

        public void SetDebugRegion(string name)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (debugDumpWriter == null)
                return;
            debugRegionName = name;
#endif
        }
        
        public void Dispose()
        {
            reader.Dispose();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (debugDumpWriter != null)
            {
                Debug.Log($"Debug dump written to {Path.GetFullPath("Temp/")}");
                debugDumpWriter.Dispose();
            }
#endif
        }
        
        public void Deserialize<T>(ref T value)
            where T : notnull
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            var start = stream.Position;
#endif
            // Native C# types
            if (typeof(T) == typeof(bool))
                value = (T)(object)reader.ReadBoolean();
            else if (typeof(T) == typeof(byte))
                value = (T)(object)reader.ReadByte();
            else if (typeof(T) == typeof(byte[]))
            {
                var length = reader.ReadInt32();
                value = (T)(object)reader.ReadBytes(length);
            }
            else if (typeof(T) == typeof(sbyte))
                value = (T)(object)reader.ReadSByte();
            else if (typeof(T) == typeof(char))
                value = (T)(object)reader.ReadChar();
            else if (typeof(T) == typeof(short))
                value = (T)(object)reader.ReadInt16();
            else if (typeof(T) == typeof(ushort))
                value = (T)(object)reader.ReadUInt16();
            else if (typeof(T) == typeof(int))
                value = (T)(object)reader.ReadInt32();
            else if (typeof(T) == typeof(uint))
                value = (T)(object)reader.ReadUInt32();
            else if (typeof(T) == typeof(float))
                value = (T)(object)reader.ReadSingle();
            else if (typeof(T) == typeof(decimal))
                value = (T)(object)reader.ReadDecimal();
            else if (typeof(T) == typeof(double))
                value = (T)(object)reader.ReadDouble();
            else if (typeof(T) == typeof(long))
                value = (T)(object)reader.ReadInt64();
            else if (typeof(T) == typeof(ulong))
                value = (T)(object)reader.ReadUInt64();
            else if (typeof(T) == typeof(string))
            {
                UInt64 length = reader.ReadUInt64();
                StringBuilder sb = new StringBuilder();
                for (UInt64 i = 0; i < length; i++)
                    sb.Append((char)reader.ReadByte());
                value = (T)(object)sb.ToString();
            }
            else if (typeof(T).IsEnum)
                value = (T)(object)reader.ReadInt32();
            
            // Unity types
            else if (typeof(T) == typeof(Vector2))
            {
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                value = (T)(object)new Vector2(x, y);
            }
            else if (typeof(T) == typeof(Vector3))
            {
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                float z = reader.ReadSingle();
                value = (T)(object)new Vector3(x, y, z);
            }
            else if (typeof(T) == typeof(Vector4))
            {
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                float z = reader.ReadSingle();
                float w = reader.ReadSingle();
                value = (T)(object)new Vector4(x, y, z, w);
            }
            else if (typeof(T) == typeof(Quaternion))
            {
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                float z = reader.ReadSingle();
                float w = reader.ReadSingle();
                value = (T)(object)new Quaternion(x, y, z, w);
            }
            else if (typeof(T) == typeof(Matrix4x4))
            {
                Matrix4x4 matrix = new Matrix4x4();
                for (int i = 0; i < 16; i++)
                {
                    matrix[i] = reader.ReadSingle();
                }
                value = (T)(object)matrix;
            }
            else if (typeof(T) == typeof(Vector2Int))
            {
                int x = reader.ReadInt32();
                int y = reader.ReadInt32();
                value = (T)(object)new Vector2Int(x, y);
            }
            else if (typeof(T) == typeof(Vector3Int))
            {
                int x = reader.ReadInt32();
                int y = reader.ReadInt32();
                int z = reader.ReadInt32();
                value = (T)(object)new Vector3Int(x, y, z);
            }
            else if (typeof(T) == typeof(BoundingSphere))
            {
                float posX = reader.ReadSingle();
                float posY = reader.ReadSingle();
                float posZ = reader.ReadSingle();
                float radius = reader.ReadSingle();
                value = (T)(object)new BoundingSphere(new Vector3(posX, posY, posZ), radius);
            }
            else if (typeof(T) == typeof(Bounds))
            {
                float centerX = reader.ReadSingle();
                float centerY = reader.ReadSingle();
                float centerZ = reader.ReadSingle();
                float sizeX = reader.ReadSingle();
                float sizeY = reader.ReadSingle();
                float sizeZ = reader.ReadSingle();
                value = (T)(object)new Bounds(new Vector3(centerX, centerY, centerZ), new Vector3(sizeX, sizeY, sizeZ));
            }
            else if (typeof(T) == typeof(Rect))
            {
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                float width = reader.ReadSingle();
                float height = reader.ReadSingle();
                value = (T)(object)new Rect(x, y, width, height);
            }
            else if (typeof(T) == typeof(BoundsInt))
            {
                int posX = reader.ReadInt32();
                int posY = reader.ReadInt32();
                int posZ = reader.ReadInt32();
                int sizeX = reader.ReadInt32();
                int sizeY = reader.ReadInt32();
                int sizeZ = reader.ReadInt32();
                value = (T)(object)new BoundsInt(new Vector3Int(posX, posY, posZ), new Vector3Int(sizeX, sizeY, sizeZ));
            }
            else if (typeof(T) == typeof(RectInt))
            {
                int x = reader.ReadInt32();
                int y = reader.ReadInt32();
                int width = reader.ReadInt32();
                int height = reader.ReadInt32();
                value = (T)(object)new RectInt(x, y, width, height);
            }
            else if (typeof(T) == typeof(Color))
            {
                float r = reader.ReadSingle();
                float g = reader.ReadSingle();
                float b = reader.ReadSingle();
                float a = reader.ReadSingle();
                value = (T)(object)new Color(r, g, b, a);
            }
            else if (typeof(T) == typeof(Color32))
            {
                byte r = reader.ReadByte();
                byte g = reader.ReadByte();
                byte b = reader.ReadByte();
                byte a = reader.ReadByte();
                value = (T)(object)new Color32(r, g, b, a);
            }
            
            // Custom types
            else if (typeof(IFoundrySerializable).IsAssignableFrom(typeof(T)))
            {
                IFoundrySerializable serializable = (IFoundrySerializable)value;
                serializable.Deserialize(this);
                value = (T)serializable;
            }
            else
                throw new NotSupportedException($"Deserialization for type {typeof(T)} is not supported. Please implement IFoundrySerializable if you are attempting to deserialize a custom type.");

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (debugDumpWriter != null)
                debugDumpWriter.WriteLine($"{start}\t{value.ToString()}\t{typeof(T).Name}\t{stream.Position - start}\t{debugRegionName}");
#endif
        }

        public MemoryStream DeserializeBuffer() 
        {
            var start = stream.Position;
            uint length = reader.ReadUInt32();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (debugDumpWriter != null)
                debugDumpWriter.WriteLine($"{start}\tRaw Buffer\tbyte[]\t{sizeof(int) + length}\t{debugRegionName}");
#endif
            return new MemoryStream(reader.ReadBytes((int)length));
        }
    }*/
}
