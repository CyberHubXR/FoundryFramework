using System;
using System.Collections.Generic;

namespace Foundry.Core.Editor
{
    public struct ProvidedService
    {
        /// <summary>
        /// Implementation specific name of the service a module provides
        /// </summary>
        public string ImplementationName;
        
        /// <summary>
        /// The interface implemented by the service
        /// </summary>
        public Type ServiceInterface;
    }
    
    public struct UsedService
    {
        /// <summary>
        /// Whether or not this system is required for the module to function
        /// </summary>
        public bool optional;
        
        /// <summary>
        /// The interface implemented by the service
        /// </summary>
        public Type ServiceInterface;
    }
    
    public interface IModuleDefinition
    {
        /// <summary>
        /// Name of the module, used for display purposes
        /// </summary>
        public string ModuleName();
        
        /// <summary>
        /// Return a list of all the services this module provides
        /// </summary>
        /// <returns></returns>
        public List<ProvidedService> GetProvidedServices();

        /// <summary>
        /// Returns a list of all the services used by this module, and if they are required or not
        /// </summary>
        public List<UsedService> GetUsedService();
        
        /// <summary>
        /// Get or create an instance of a module's config object
        /// </summary>
        public FoundryModuleConfig GetModuleConfig();
    }
}
