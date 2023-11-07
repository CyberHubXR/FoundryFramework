using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Foundry.Core.Serialization;
using Foundry.Networking;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Foundry
{
    public enum NetEventSource
    {
        Local,
        Remote
    }

    [Serializable]
    public class NetworkEventBase
    {
        
    }
    
    /// <summary>
    /// This event is synced across the network. In most ways it functions identically to UnityEvents, but it is even less
    /// performant because of the serialization and network overhead. Use only for events that need to be synced across the network.
    /// </summary>
    /// <typeparam name="T">T must be a commonly serializable type or implement IFoundrySerializable</typeparam>
    [Serializable]
    public class NetworkEvent<T> : NetworkEventBase, INetworkProperty
    {
        private uint _maxQueueLength = 5;
        
        private Queue<byte[]> _callArgs = new();
        [SerializeField]
        private UnityEvent<NetEventSource, T> _event = new();

        
        /// <summary>
        /// The max amount of events that may be queued up between serializations. If this is exceeded, the oldest events will be removed.
        /// </summary>
        public uint MaxQueueLength
        {
            get => _maxQueueLength;
            set
            {
                _maxQueueLength = value;
                
                // Remove the oldest events if we are over the max queue length
                while (_callArgs.Count > _maxQueueLength)
                    _callArgs.Dequeue();
            }
        }
        
        public bool Dirty =>  _callArgs.Count > 0;
        public void SetDirty()
        {
            
        }

        public void SetClean()
        {
            
        }

        public void Serialize(FoundrySerializer serializer)
        {
            serializer.SetDebugRegion($"NetworkEvent<{typeof(T).Name}>");
            int count = _callArgs.Count;
            serializer.Serialize(in count);
            while (_callArgs.Count > 0)
            {
                var arg = _callArgs.Dequeue();
                serializer.Serialize(in arg);
            }
        }

        public void Deserialize(FoundryDeserializer deserializer)
        {
            deserializer.SetDebugRegion($"NetworkEvent<{typeof(T).Name}>");
            int count = 0;
            deserializer.Deserialize(ref count);
            for (int i = 0; i < count; i++)
            {
                byte[] arg = null;
                deserializer.Deserialize(ref arg);
                MemoryStream stream = new(arg);
                FoundryDeserializer argDeserializer = new(stream);
                T argValue = default;
                argDeserializer.Deserialize(ref argValue);
                _event.Invoke(NetEventSource.Remote, argValue);
            }
        }

        /// <summary>
        /// Never is called for NetworkEvent
        /// </summary>
        public Action OnChanged { get; set; }

        private void EnqueueNetCall(T arg)
        {
            MemoryStream stream = new();
            FoundrySerializer serializer = new FoundrySerializer(stream);
            serializer.Serialize(arg);
            _callArgs.Enqueue(stream.GetBuffer());

            if (_callArgs.Count > _maxQueueLength)
                _callArgs.Dequeue();
        }
        
        /// <summary>
        /// Add a listener to this event. This is called when the event is invoked. Use NetEventSource to filter between local and remote calls.
        /// </summary>
        /// <param name="call"></param>
        public void AddListener(UnityAction<NetEventSource, T> call)
        {
            _event.AddListener(call);
        }
        
        public void RemoveListener(UnityAction<NetEventSource, T> call)
        {
            _event.RemoveListener(call);
        }
        
        /// <summary>
        /// Invoke this event. This will call all listeners on all clients.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="arg"></param>
        public void Invoke(T arg)
        {
            EnqueueNetCall(arg);
            _event.Invoke(NetEventSource.Local, arg);
        }

        /// <summary>
        /// Invoke this event locally. This will only call listeners on the local client.
        /// </summary>
        /// <param name="arg"></param>
        public void InvokeLocal(T arg)
        {
            _event.Invoke(NetEventSource.Local, arg);
        }
        
        /// <summary>
        /// Invoke this event remotely. This will only call listeners on remote clients.
        /// </summary>
        /// <param name="arg"></param>
        public void InvokeRemote(T arg)
        {
            EnqueueNetCall(arg);
        }
    }
    
    
    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(NetworkEventBase), true)]
    public class NetworkEventDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("_event"), label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(position, property.FindPropertyRelative("_event"), label);
            EditorGUI.EndProperty();
        }
    }
    #endif
}
