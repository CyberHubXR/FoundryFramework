using System;
using CyberHub.Foundry.Editor;

namespace Foundry.Editor
{
    public class PlayerRigManagerDefinition : IServiceDefinition
    {

        public string Source()
        {
            return "com.cyberhub.foundry.core";
        }

        public string PrettyName()
        {
            return "Player Rig Manager";
        }

        public Type ServiceInterface()
        {
            return typeof(IPlayerRigManager);
        }
    }
}
