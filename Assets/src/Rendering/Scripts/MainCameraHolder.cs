using System.Collections;
using System.Collections.Generic;
using CyberHub.Brane;
using UnityEngine;

namespace Foundry
{
    public class MainCameraHolder : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            var mainCameraManager = BraneApp.GetService<IFoundryCameraManager>();
            var camera = mainCameraManager.MainCamera;
            camera.transform.SetParent(transform, false);
            camera.transform.localPosition = Vector3.zero;
            camera.transform.localRotation = Quaternion.identity;
        }
    }
}

