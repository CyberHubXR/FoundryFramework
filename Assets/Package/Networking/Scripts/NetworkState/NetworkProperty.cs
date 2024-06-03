using System;
using System.IO;
using Foundry.Core.Serialization;
using UnityEngine;

namespace Foundry.Networking
{
    [Serializable]
    public class NetworkProperty<T> : INetworkProperty
    {
        [SerializeField]
        private T value;
        private bool dirty;
        private IFoundrySerializer tSerializer;
        
        /// <summary>
        /// Invoked when the value of this property changes, either locally or remotely.
        /// </summary>
        public Action<T> OnValueChanged;
        
        /// <inheritdoc cref="INetworkProperty"/>
        public Action OnChanged { get; set; }

        public T Value
        {
            get => value;
            set
            {
                if(!Equals(this.value, value))
                    dirty = true;
                this.value = value;
                try
                {
                    OnValueChanged?.Invoke(value);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public NetworkProperty()
        {
            if (value is IFoundrySerializable serializable)
                tSerializer = serializable.GetSerializer();
            else
                tSerializer =  FoundrySerializerFinder.GetSerializer(typeof(T));
            Debug.Assert(tSerializer != null, $"Serializer for {typeof(T)} was null");
        }
        
        public NetworkProperty(T defaultValue)
        {
            value = defaultValue;
            if (defaultValue is IFoundrySerializable serializable)
                tSerializer = serializable.GetSerializer();
            else
                tSerializer =  FoundrySerializerFinder.GetSerializer(typeof(T));
            Debug.Assert(tSerializer != null, $"Serializer for {typeof(T)} was null");
            OnValueChanged += _ => OnChanged?.Invoke();
        }

        public bool Dirty => dirty;
        
        public void SetDirty()
        {
            dirty = true;
        }
        
        public void SetClean()
        {
            dirty = false;
        }
        private struct Serializer : IFoundrySerializer
        {
            public void Serialize(in object value, BinaryWriter writer)
            {
                var prop = (NetworkProperty<T>) value;
                prop.tSerializer.Serialize(prop.value, writer);
            }

            public void Deserialize(ref object value, BinaryReader reader)
            {
                var prop = (NetworkProperty<T>) value;
                object obj = prop.value;
                prop.tSerializer.Deserialize(ref obj, reader);
                prop.value = (T) obj;
                try
                {
                    prop.OnValueChanged?.Invoke(prop.value);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public IFoundrySerializer GetSerializer()
        {
            return new Serializer();
        }

        public override string ToString()
        {
            return "Networked Property: " + value + (dirty ? " (Dirty)" : " (Clean)");
        }
    }
}
