using System.Collections;
using System.Collections.Generic;
using CyberHub.Brane;
using Foundry;
using UnityEngine;

namespace Foundry
{
    [RequireComponent(typeof(Camera))]
    public class FoundryMainCamera : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            BraneApp.GetService<IFoundryCameraManager>().MainCamera = GetComponent<Camera>();
        }
    }
}

