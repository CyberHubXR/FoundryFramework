using System;
using CyberHub.Foundry.Editor;

namespace Foundry.Services.Editor
{
    public class SceneNavigatorDefinition : IServiceDefinition
    {
        public string Source()
        {
            return "com.cyberhub.foundry.core";
        }

        public string PrettyName()
        {
            return "Scene Navigator";
        }

        public Type ServiceInterface()
        {
            return typeof(ISceneNavigator);
        }
    }
}

