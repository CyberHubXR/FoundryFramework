using System;
using System.Collections.Generic;

namespace Foundry.Core.Editor
{
    public static class ClassFinder
    {
        /// <summary>
        /// Find all classes that have the given attribute
        /// </summary>
        /// <param name="attributes">The attribute types to search for</param>
        /// <returns></returns>
        public static List<Type> FindAllWithAttributes(Type[] attributes)
        {
            
            var types = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach(var attribute in attributes)
                    {
                        if (type.IsDefined(attribute, false))
                            types.Add(type);
                    }
                }
            }

            return types;
        }
        
        /// <summary>
        /// Find all types that inherit from T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<Type> FindAllWithParent<T>()
        {
            var types = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsSubclassOf(typeof(T)) && !type.IsAbstract)
                        types.Add(type);
                }
            }

            return types;
        }
        
        /// <summary>
        /// Find all types that implement T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<Type> FindAllWithInterface<T>()
        {
            var types = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(T).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                        types.Add(type);
                }
            }

            return types;
        }
    }
}


