using System;
using CyberHub.Brane;
using Foundry;

using UnityEngine;

public class Billboard : FoundryScript
{
    private Transform mainCamera;

    public void Start()
    {
        var cameraManager = BraneApp.GetService<IFoundryCameraManager>();
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
