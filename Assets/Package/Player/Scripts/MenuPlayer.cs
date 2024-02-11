using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    public class MenuPlayer : MonoBehaviour
    {
        private IPlayerControlRig _controlRig;
        public GameObject leftHand;
        public GameObject rightHand;

        private void LoadPlayerRig()
        {
            _controlRig.transform.SetParent(transform, false);
            _controlRig.transform.localPosition = Vector3.zero;
            _controlRig.transform.localRotation = Quaternion.identity;
            if (_controlRig == null)
                Debug.LogError("PlayerControlRig was not found, has one been created? Make sure there is only one script using it at a time.");
            
            //If this is a desktop rig, change the camera mode and reset rotation
            if (_controlRig is DesktopControlRig)
            {
                DesktopControlRig rig = (DesktopControlRig)_controlRig;
                rig.SetCameraMode(DesktopControlRig.CameraMode.InteractUI);
                rig.neckPivot.localRotation = Quaternion.identity;
            }
        }
        
        private void Start()
        {
            var rigManager = FoundryApp.GetService<IPlayerRigManager>();
            if (rigManager.Rig != null)
            {
                _controlRig = rigManager.BorrowPlayerRig();
                LoadPlayerRig();
            }
            else
            {
                rigManager.PlayerRigCreated += rig =>
                {
                    _controlRig = rigManager.BorrowPlayerRig();
                    LoadPlayerRig();
                };
            }

        }

        private void OnDestroy()
        {
            if (_controlRig != null)
            {
                var rigManager = FoundryApp.GetService<IPlayerRigManager>();
                rigManager.ReturnPlayerRig();
            }
        }

        void Update()
        {
            if (_controlRig == null)
                return;
            var targets = new TrackerPos[6];
            _controlRig.UpdateTrackers(targets);
            leftHand.transform.localPosition = targets[1].translation;
            leftHand.transform.localRotation = targets[1].rotation;
            rightHand.transform.localPosition = targets[2].translation;
            rightHand.transform.localRotation = targets[2].rotation;
        }
    }
}

