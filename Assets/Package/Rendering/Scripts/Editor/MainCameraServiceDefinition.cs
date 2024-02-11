using System;
using System.Collections;
using System.Collections.Generic;
using Foundry.Core.Editor;
using UnityEngine;

namespace Foundry.Rendering.Editor
{
    public class MainCameraServiceDefinition : IServiceDefinition
    {
        public string Source()
        {
            return "com.cyberhub.foundry.core";
        }

        public string PrettyName()
        {
            return "Camera Manager";
        }

        public Type ServiceInterface()
        {
            return typeof(IFoundryCameraManager);
        }
    }
}

