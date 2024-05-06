using System.Collections;
using System.Collections.Generic;
using CyberHub.Brane;
using CyberHub.Brane.Editor;
using Foundry.Core.Editor;
using Foundry.Networking;
using Foundry.Services;
using UnityEngine;

namespace Foundry
{
    public class FoundryCoreModuleDefinition: IModuleDefinition
    {
        public string ModuleName()
        {
            return "Foundry Core";
        }

        public List<ProvidedService> GetProvidedServices()
        {
            return new List<ProvidedService>
            {
                new ProvidedService
                {
                    ImplementationName = "Core Scene Navigator",
                    ServiceInterface = typeof(ISceneNavigator)
                },
                new ProvidedService
                {
                    ImplementationName = "Core Player Rig Manager",
                    ServiceInterface = typeof(IPlayerRigManager)
                },
                new ProvidedService
                {
                    ImplementationName = "Core Camera Manager",
                    ServiceInterface = typeof(IFoundryCameraManager)
                }
            };
        }

        public List<UsedService> GetUsedServices()
        {
            return new List<UsedService>
            {
                new UsedService
                {
                    optional = false,
                    ServiceInterface = typeof(ISceneNavigator)
                },
                new UsedService
                {
                    optional = false,
                    ServiceInterface = typeof(IPlayerRigManager)
                },
                new UsedService
                {
                    optional = false,
                    ServiceInterface = typeof(IFoundryCameraManager)
                },
                new UsedService
                {
                    optional = false,
                    ServiceInterface = typeof(INetworkProvider)
                }
            };
        }

        public BraneModuleConfig GetModuleConfig()
        {
            return FoundryCoreConfig.GetAsset();
        }
    }
}