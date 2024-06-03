using System;
using System.Collections.Specialized;
using System.IO;
using Foundry.Core.Serialization;
using UnityEngine;

namespace Foundry.Networking
{
    [Serializable]
    public class NetworkArray<T> : INetworkProperty
    {
        // Serializable only for debugging purposes
        [SerializeField]
        private T[] data;
        
        private IFoundrySerializer tSerializer;
        
        private uint dirtyItems;
        private BitVector32[] dirtyFlags;
        
        /// <summary>
        /// Called when a value is set on the array, both locally and over the network. The index and value are passed as parameters.
        /// </summary>
        public Action<int, T> OnIndexSet;

        public Action OnChanged { get; set; }

        /// <summary>
        /// To keep serialization efficient, a size is required that should be the same on all clients. The size is not networked.
        /// </summary>
        /// <param name="size"></param>
        public NetworkArray(int size)
        {
            Debug.Assert(size > 0, "Cannot create a NetworkArray with a size of 0");
            data = new T[size];
            
            if (data[0] is IFoundrySerializable d)
                tSerializer = d.GetSerializer();
            else 
                tSerializer = FoundrySerializerFinder.GetSerializer(typeof(T));
            Debug.Assert(tSerializer != null, $"Serializer for {typeof(T)} was null");
            
            dirtyFlags = new BitVector32[size / 32 + 1];
            Array.Fill(dirtyFlags, new BitVector32());
            dirtyItems = 0;
            OnIndexSet += (index, value) => OnChanged?.Invoke();
        }

        /// <summary>
        /// Initialize a NetworkArray with a default value. The size is not networked.
        /// </summary>
        /// <param name="defaultValue"></param>
        /// <param name="size"></param>
        public NetworkArray(T defaultValue, int size)
        {
            data = new T[size];
            Array.Fill(data, defaultValue);
            dirtyFlags = new BitVector32[size / 32 + 1];
            Array.Fill(dirtyFlags, new BitVector32());
            dirtyItems = 0;
            OnIndexSet += (index, value) => OnChanged?.Invoke();
        }
        

        /// <summary>
        /// Construct a NetworkArray with a default value for each item. The size is not networked.
        /// </summary>
        /// <param name="defaultValues"></param>
        public NetworkArray(T[] defaultValues)
        {
            data = defaultValues;
            dirtyFlags = new BitVector32[defaultValues.Length / 32 + 1];
            Array.Fill(dirtyFlags, new BitVector32());
            dirtyItems = 0;
            OnIndexSet += (index, value) => OnChanged?.Invoke();
        }

        public bool Dirty => dirtyItems > 0;
        
        public int Length => data.Length;
        
        public void SetDirty()
        {
            dirtyItems = (uint)data.Length;
            for (int i = 0; i < dirtyFlags.Length; i++)
                dirtyFlags[i][Int32.MaxValue] = true;
        }
        
        public void SetClean()
        {
            // Clear individual dirty flags
            dirtyItems = 0;
            for (int i = 0; i < dirtyFlags.Length; i++)
                dirtyFlags[i] = new BitVector32(0);
        }

        public void SetIndexDirty(int index)
        {
            int bitmask = 1 << (index % 32);
            int vectorIndex = index / 32;
            if (dirtyFlags[vectorIndex][bitmask])
                return;
            
            dirtyItems += 1;
            dirtyFlags[vectorIndex][bitmask] = true;
        }

        public bool IsIndexDirty(int index)
        {
            int bitmask = 1 << (index % 32);
            int vectorIndex = index / 32;
            return dirtyFlags[vectorIndex][bitmask];
        }

        public IFoundrySerializer GetSerializer()
        {
            return new Serializer();
        }

        private struct Serializer: IFoundrySerializer
        {
            public void Serialize(in object obj, BinaryWriter writer)
            {
                var array = (NetworkArray<T>)obj;
                uint items = (uint)array.data.Length;
                writer.Write(items);
            
                for (int i = 0; i < array.data.Length; i++)
                    array.tSerializer.Serialize(array.data[i], writer);
            }

            public void Deserialize(ref object obj, BinaryReader reader)
            {
                var array = (NetworkArray<T>)obj;
                uint length = reader.ReadUInt32();
                for (uint i = 0; i < length; i++)
                {
                    object v = array.data[i];
                    array.tSerializer.Deserialize(ref v, reader);
                    array.data[i] = (T)v;
                }
            }
        }
        
        public void Set(int index, T value)
        {
            if(!Equals(data[index], value))
                SetIndexDirty(index);
            data[index] = value;
            OnIndexSet?.Invoke(index, data[index]);
        }
        
        public T Get(int index)
        {
            return data[index];
        }
        
        public T this[int index]
        {
            get => data[index];
            set => Set(index, value);
        }
        
        /// <summary>
        /// It is advised to use the provided Set, Get, and [] methods instead of this, as they handle setting the dirty flags for you.
        /// But if you want to edit items manually or by reference you can use this. Make sure to set the dirty flag manually
        /// for indices you change using SetIndexDirty().
        /// </summary>
        public T[] RawArray => data;


        public override string ToString()
        {
            string result = "[";
            for (int i = 0; i < data.Length; i++)
            {
                result += data[i].ToString();
                if(IsIndexDirty(i))
                    result += "*";
                if (i < data.Length - 1)
                    result += ", ";
            }
            result += "]";
            return result;
        }
    }
}
