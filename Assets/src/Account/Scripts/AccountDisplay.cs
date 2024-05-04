using CyberHub.Brane;
using UnityEngine;
using TMPro;
using Foundry.Networking;

namespace Foundry.Account
{
    public class AccountDisplay : NetworkComponent
    {
        public TMP_Text usernameDisplay;
        public Transform head;
        public Transform usernameCanvas;
        public Transform billboardText;

        private Transform mainCamera;

        public override void OnConnected()
        {
            var cameraManager = BraneApp.GetService<IFoundryCameraManager>();
            mainCamera = cameraManager.MainCamera.transform;
            if (!IsOwner)
                return;


            Destroy(usernameCanvas.gameObject);
            // We can't call destroy on this object, since that would leave a null reference Fusion's NetworkBehaviour list.
            enabled = false;
        }

        private void LateUpdate()
        {
            if (!mainCamera)
                return;
            usernameCanvas.position = head.position;
            billboardText.transform.LookAt(billboardText.position + mainCamera.rotation * Vector3.forward, Vector3.up);
        }

        public void SetText(string text)
        {
            usernameDisplay.text = text;
        }
    }

}