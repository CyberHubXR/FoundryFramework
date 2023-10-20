using System;
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
                OnValueChanged?.Invoke(value);
            }
        }
        
        public NetworkProperty(T defaultValue)
        {
            value = defaultValue;
            OnValueChanged += (value) => OnChanged?.Invoke();
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
        
        public void Serialize(FoundrySerializer serializer)
        {
            serializer.SetDebugRegion($"NetworkProperty<{typeof(T).Name}>");
            serializer.Serialize(in value);
        }

        public void Deserialize(FoundryDeserializer deserializer)
        {
            deserializer.SetDebugRegion($"NetworkProperty<{typeof(T).Name}>");
            deserializer.Deserialize(ref value);
            OnValueChanged?.Invoke(value);
        }

        public override string ToString()
        {
            return "Networked Property: " + value + (dirty ? " (Dirty)" : " (Clean)");
        }
    }
}
