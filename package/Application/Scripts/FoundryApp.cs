using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Foundry.Services;
using UnityEngine;

namespace Foundry
{
    /// <summary>
    /// The core API for Foundry. This class is responsible for managing systems and services.
    /// </summary>
    public class FoundryApp
    {
        private static FoundryApp _instance = new ();
        
        /// <summary>
        /// Where modular singleton services are stored.
        /// </summary>
        private readonly ServiceContainer services = new();
        
        private FoundryAppConfig config;
        private readonly Dictionary<Type, FoundryModuleConfig> moduleConfigs = new();

        internal FoundryApp()
        {
            // Add config services
            config = Resources.Load<FoundryAppConfig>("FoundryAppConfig");
            Debug.Assert(config, "FoundryAppConfig not found!");
            config.RegisterServices(this);
            foreach(var module in config.modules)
                moduleConfigs.Add(module.GetType(), module);
        }
        
        /// <summary>
        /// Add a service to the Foundry instance.
        /// </summary>
        /// <param name="type">Interface this service implements</param>
        /// <param name="service">Class instance that implements the interface</param>
        public void AddService(Type type, object service)
        {
            Debug.Assert(services.GetService(type) == null, "Service implementing " + type.Name +" already exists!");
            services.AddService(type, service);
        }
        
        /// <summary>
        /// Get a foundry service. Returns an error if the service was not found.
        /// </summary>
        /// <param name="type">Interface to get</param>
        /// <returns></returns>
        public object GetService(Type type)
        {
            var result = services.GetService(type);
            Debug.Assert(result != null, "Service implementing " + type.Name + " not found!");
            return result;
        }

        /// <summary>
        /// Try getting a foundry service.
        /// </summary>
        /// <param name="type">Interface type of the service</param>
        /// <param name="service">returned service, null if not found</param>
        /// <returns>true if the service was found</returns>
        public bool TryGetService(Type type, out object service)
        {
            service = services.GetService(type);
            return service != null;
        }

        /// <summary>
        /// Add a service to the Foundry instance.
        /// </summary>
        /// <typeparam name="I">Interface that this class implements</typeparam>
        /// <typeparam name="T">Service implementation to be instantiated</typeparam>
        public void AddService<I, T>()
            where T: new()
            => _instance.AddService(typeof(I), new T());
        
        /// <summary>
        /// Add a service to the Foundry instance.
        /// </summary>
        /// <typeparam name="I">Interface that this class implements</typeparam>
        /// <typeparam name="T">Service implementation to be added</typeparam>
        public void AddService<I, T>(T service)
            where T: new()
            => _instance.AddService(typeof(I), service);
        
        /// <summary>
        /// Get a foundry service. 
        /// </summary>
        /// <typeparam name="T">Interface for the service you want</typeparam>
        /// <returns></returns>
        public static T GetService<T>()
            where T: class
            => _instance.GetService(typeof(T)) as T;

        /// <summary>
        /// Try getting a foundry service. Returns true if the service was found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool TryGetService<T>(out T service)
            where T : class
        {
            if (_instance.TryGetService(typeof(T), out object s))
            {
                service = s as T;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Get the config object for a loaded module.
        /// </summary>
        /// <typeparam name="T">class deriving from FoundryModuleConfig</typeparam>
        /// <returns>Returns a config object, or null if it was not found</returns>
        public static T GetConfig<T>()
        where T : FoundryModuleConfig
        {
            if(_instance.moduleConfigs.TryGetValue(typeof(T), out FoundryModuleConfig config))
                return config as T;
            return null;
        }
            

        /// <summary>
        /// The currently active services running in the Foundry instance.
        /// </summary>
        public static ServiceContainer Services => _instance.services;
    }
}