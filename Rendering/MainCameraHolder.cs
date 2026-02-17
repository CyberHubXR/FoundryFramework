using System.Collections;
using System.Collections.Generic;
using CyberHub.Foundry;
using UnityEngine;

namespace Foundry
{
    public class MainCameraHolder : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            var mainCameraManager = FoundryApp.GetService<IFoundryCameraManager>();
            var camera = mainCameraManager.MainCamera;
            camera.transform.SetParent(transform, false);
            camera.transform.localPosition = Vector3.zero;
            camera.transform.localRotation = Quaternion.identity;
        }
    }
}

