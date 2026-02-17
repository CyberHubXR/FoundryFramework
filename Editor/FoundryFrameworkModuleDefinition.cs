using System.Collections.Generic;
using CyberHub.Foundry;
using CyberHub.Foundry.Editor;
using Foundry.Services;

namespace Foundry
{
    public class FoundryFrameworkModuleDefinition: IModuleDefinition
    {
        public string ModuleName()
        {
            return "Foundry Framework";
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
                }
            };
        }

        public FoundryModuleConfig GetModuleConfig()
        {
            return FoundryFrameworkConfig.GetAsset();
        }
    }
}