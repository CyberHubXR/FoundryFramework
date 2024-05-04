using System;
using System.Collections;
using System.Collections.Generic;
using CyberHub.Brane.Editor;
using UnityEngine;

namespace Foundry.Networking.Editor
{
    public class NetworkSystemDefinition : IServiceDefinition
    {
        public string Source()
        {
            return "com.cyberhub.foundry.core";
        }

        public string PrettyName()
        {
            return "Network Provider";
        }

        public Type ServiceInterface()
        {
            return typeof(INetworkProvider);
        }
    }
}
