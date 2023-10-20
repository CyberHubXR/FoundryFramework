using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Foundry
{

    [Serializable]
    public class MappedPropertyImpl
    {
        /// <summary>
        /// This is the name of the implementation this data was serialized for, in the case of networking this might be
        /// Photon Fusion, Mirror, Normcore etc.
        /// </summary>
        public string implementationId;

        /// <summary>
        /// This is the raw serialized data for the property. Each network provider should provide CustomPropertyDrawers
        /// for their own data types.
        /// </summary>
        [Tooltip("We recommend that this should not be edited directly, but instead through a property drawer provided by a relevant package.")]
        public byte[] data;
    }

    [Serializable]
    public class MappedProperties
    {
        /// <summary>
        /// The serialized data for all properties on this component. This is a list because we may have multiple implementations.
        /// </summary>
        ///
        [Header("DO NOT EDIT")]
        [Tooltip("We recommend that this should not be edited directly, but instead through a property drawer provided by a relevant package.")]
        public List<MappedPropertyImpl> serializedProperties = new();
        
        /// <summary>
        /// We attempt to cache on property lookup, this is the cached value. This is used to avoid having to do a lookup,
        /// since as of now there are no cases where we have multiple implementations of the same property active at once.
        /// </summary>
        private object propCache;
        
        /// <summary>
        /// Attempts to get the properties for the given implementation. If no properties are found, returns the default value for
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetProps<T>(string implementationId)
        where T: class, new()
        {
            if (propCache is T cache)
                return cache;
            
            foreach (var impl in serializedProperties)
            {
                if (impl.implementationId == implementationId)
                {
                    var stream = new MemoryStream(impl.data);
                    BinaryFormatter bf = new();

                    try
                    {
                        propCache = bf.Deserialize(stream);
                    }
                    catch (Exception e)
                    {
                        impl.data = new byte[0];
                        return new T();
                    }
                    
                    return propCache as T;
                }
            }
            return new();
        }

        public void SetProps<T>(string implementationId, T value)
        where T: class
        {
            
            var stream = new MemoryStream();
            BinaryFormatter bf = new();
            bf.Serialize(stream, value);
            var data = stream.GetBuffer();
            
            foreach (var impl in serializedProperties)
            {
                if (impl.implementationId == implementationId)
                {
                    impl.data = data;
                    return;
                }
            }
            
            serializedProperties.Add(new MappedPropertyImpl()
            {
                implementationId = implementationId,
                data = data
            });
                
        }
    }
}
