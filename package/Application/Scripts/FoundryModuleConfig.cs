using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Foundry
{
    /// <summary>
    /// Modules override this class as a place to store settings specific to them, and to register services with FoundryCore.
    /// </summary>
    public abstract class FoundryModuleConfig : ScriptableObject
    {
        public delegate object ServiceConstructor();

        [Serializable]
        public class SerializedType
        {
            [SerializeField]
            private string AssemblyQualifiedName;

            public Type Type
            {
                get => Type.GetType(AssemblyQualifiedName);
                set => AssemblyQualifiedName = value.AssemblyQualifiedName;
            }
            
            public static implicit operator Type(SerializedType serializedType)
            {
                return serializedType.Type;
            }
            
            public static implicit operator SerializedType(Type type)
            {
                return new SerializedType {Type = type};
            }
            
            public static bool operator==(SerializedType a, SerializedType b)
            {
                return a.AssemblyQualifiedName == b.AssemblyQualifiedName;
            }

            public static bool operator !=(SerializedType a, SerializedType b)
            {
                return !(a == b);
            }
            
            public static bool operator==(SerializedType a, Type b)
            {
                return a.AssemblyQualifiedName == b.AssemblyQualifiedName;
            }

            public static bool operator !=(SerializedType a, Type b)
            {
                return !(a == b);
            }

            public override string ToString()
            {
                return AssemblyQualifiedName;
            }
        }
        
        /// <summary>
        /// List of full paths to system interfaces enabled for this module
        /// </summary>
        public List<SerializedType> EnabledServices;

        internal void RegisterServices(FoundryApp instance)
        {
            Dictionary<Type, ServiceConstructor> constructors = new();
            RegisterServices(constructors);
            
            foreach (var system in EnabledServices)
            {
                Type systemType = system;
                Debug.Assert(systemType != null, $"Could not find type {system}!");
                Debug.Assert(constructors.ContainsKey(systemType), $"{GetType().Name} did not provide a constructor for {systemType.Name}!");
                instance.AddService(systemType, constructors[systemType]());
            }
        }
        
        /// <summary>
        /// Returns true if a system is enabled for this module
        /// </summary>
        /// <param name="systemType"></param>
        public bool IsServiceEnabled(Type systemType)
        {
            foreach (var system in EnabledServices)
            {
                if (system == systemType)
                    return true;
            }
            return false;
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// Enable a system for this module
        /// </summary>
        /// <param name="systemType"></param>
        public void EnableService(Type systemType)
        {
            if (!IsServiceEnabled(systemType))
            {
                EnabledServices.Add(systemType);
                EditorUtility.SetDirty(this);
            }
        }
        
        /// <summary>
        /// Disable a system for this module
        /// </summary>
        /// <param name="systemType"></param>
        public void DisableService(Type systemType)
        {
            if (IsServiceEnabled(systemType))
            {
                EnabledServices.Remove(systemType);
                EditorUtility.SetDirty(this);
            }
        }
        #endif
        
        /// <summary>
        /// Called on startup to register service constructors with FoundryCore.
        /// </summary>
        /// <param name="constructors">Dictionary to add service constructors too. These will be called conditionally
        /// depending on the project config. </param>
        public abstract void RegisterServices(Dictionary<Type, ServiceConstructor> constructors);
    }
    
    #if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(FoundryModuleConfig), true)]
    public class FoundryModuleConfigEditor : UnityEditor.Editor
    {
        public bool showEnabledServices = false;
        public override void OnInspectorGUI()
        {
            var config = (FoundryModuleConfig) target;

            showEnabledServices = EditorGUILayout.BeginFoldoutHeaderGroup(showEnabledServices, "Enabled Services");
            if (showEnabledServices)
            {
                EditorGUI.BeginDisabledGroup(true);
                foreach(var service in config.EnabledServices)
                    EditorGUILayout.LabelField(service.ToString());
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
    #endif
}
