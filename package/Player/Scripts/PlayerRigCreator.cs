using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Foundry;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.XR.Management;

namespace Foundry
{
    public enum PlayerControlMode
    {
        Auto,
        Desktop,
        XR
    }

    public class PlayerRigCreator : MonoBehaviour
    {
        [FormerlySerializedAs("DesktopRig")] public GameObject desktopRig;
        [FormerlySerializedAs("XrRig")] [FormerlySerializedAs("XRRig")] public GameObject xrRig;

        public PlayerControlMode controlMode;
        private static PlayerControlMode initializedControlMode;

        IEnumerator InitalizeControlRig()
        {
            // Start XR if we have that selected
            PlayerControlMode targetMode = controlMode;
            if (targetMode != PlayerControlMode.Desktop)
            {
                if(initializedControlMode != PlayerControlMode.XR)
                {
                    var manager = XRGeneralSettings.Instance.Manager;
                    if (!manager.activeLoader)
                    {
                        yield return manager.InitializeLoader();
                        if (manager.activeLoader != null)
                            manager.StartSubsystems();
                        else if (targetMode == PlayerControlMode.Auto)
                            targetMode = PlayerControlMode.Desktop;
                        else
                            throw new System.Exception("Could not initalize requested control mode!");
                    }
                }
                initializedControlMode = PlayerControlMode.XR;
            }
            if (targetMode == PlayerControlMode.Desktop)
                initializedControlMode = PlayerControlMode.Desktop;

            // Spawn the control rig for the selected mode
            GameObject selectedRig = (initializedControlMode == PlayerControlMode.Desktop) ? desktopRig : xrRig;
            GameObject rig = Instantiate(selectedRig, transform.position, transform.rotation, transform);
            FoundryApp.GetService<IPlayerRigManager>().RegisterRig(rig.GetComponent<IPlayerControlRig>(), transform);
        }
        
        public void Start()
        {
            StartCoroutine(InitalizeControlRig());
        }

        private void OnApplicationQuit()
        {
            var manager = XRGeneralSettings.Instance.Manager;
            if(manager.activeLoader)
            {
                manager.StopSubsystems();
                manager.DeinitializeLoader();
            }
        }
    }

}
