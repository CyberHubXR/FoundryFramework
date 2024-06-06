using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using CyberHub.Brane;
using Foundry;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace Foundry
{
    public enum PlayerControlMode
    {
        Auto,
        Desktop,
        XR,
        Bot
    }

    public class PlayerRigCreator : MonoBehaviour
    {
        public GameObject desktopRig;
        public GameObject xrRig;
        [Tooltip("The bot rig is used for testing purposes, it will be used if the selected control mode is XR and XR is not enabled.")]
        public GameObject botRig;

        public PlayerControlMode controlMode;
        private static PlayerControlMode initializedControlMode;

        IEnumerator InitalizeControlRig()
        {
            // Start XR if we have that selected
            PlayerControlMode targetMode = controlMode;
            if (targetMode == PlayerControlMode.Bot)
            {
                GameObject bot = Instantiate(botRig, transform.position, transform.rotation, transform);
                BraneApp.GetService<IPlayerRigManager>().RegisterRig(bot.GetComponent<IPlayerControlRig>(), transform);
                yield break;
            }
            
            if (targetMode != PlayerControlMode.Desktop)
            {
                if (initializedControlMode != PlayerControlMode.XR)
                {
                    var instance = XRGeneralSettings.Instance;
                    Debug.Assert(instance, "XR Settings not found, Make sure XR is installed and enabled!");
                    var manager = instance.Manager;
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
            BraneApp.GetService<IPlayerRigManager>().RegisterRig(rig.GetComponent<IPlayerControlRig>(), transform);
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
