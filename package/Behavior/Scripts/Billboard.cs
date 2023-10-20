using System;
using Foundry;

using Fusion;
using UnityEngine;

public class Billboard : NetworkBehaviour
{
    private Transform mainCamera;

    public override void Spawned()
    {
        var cameraManager = FoundryApp.GetService<IFoundryCameraManager>();
        mainCamera = cameraManager.MainCamera.transform;
    }

    private void LateUpdate()
    {
        if (!mainCamera)
            return;

        Vector3 direction = transform.position - mainCamera.position;

        transform.rotation = Quaternion.LookRotation(-Vector3.up, -direction);
    }
}
