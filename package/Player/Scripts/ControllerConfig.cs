using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace Foundry
{
    [CreateAssetMenu(fileName = "ControllerConfig", menuName = "Foundry/ControllerConfig", order = 2)]
    public class ControllerConfig : ScriptableObject
    {
        [System.Serializable]
        public struct ControllerOffset
        {
            public PosRot leftOffset;
            public PosRot rightOffset;

        }

        [System.Serializable]
        public class DeviceOffset
        {
            public string name;
            public PosRot headOffset = new PosRot
            {
                rot = Quaternion.identity
            };

            public ControllerOffset controllerOffsets = new ControllerOffset
            {
                leftOffset = new PosRot
                {
                    rot = Quaternion.identity
                },
                rightOffset = new PosRot
                {
                    rot = Quaternion.identity
                }
            };
        }

        [Tooltip("Lookup is done by device name, if there are no matches the first in the list is used as default")]
        public List<DeviceOffset> devices;

        public DeviceOffset GetCurrentDeviceOffsets()
        {
            // No VR, no offset
            if (XRGeneralSettings.Instance.Manager.activeLoader == null)
                return new DeviceOffset
                {
                    name = "desktop",
                    headOffset = new PosRot
                    {
                        rot = Quaternion.identity
                    },
                    controllerOffsets = new ControllerOffset
                    {
                        rightOffset = new PosRot
                        {
                            rot = Quaternion.identity
                        },
                        leftOffset = new PosRot
                        {
                            rot = Quaternion.identity
                        }
                    }
                };

            List<InputDevice> XRDevices = new List<InputDevice>();
            InputDevices.GetDevices(XRDevices);

            DeviceOffset defaultDevice = devices[0];
            foreach (var xrDevice in XRDevices)
            {
                if ((xrDevice.characteristics & InputDeviceCharacteristics.HeldInHand) == 0)
                    continue;
                Debug.Log("Detected " + xrDevice.name + ", attempting to find associated offsets");
                foreach (DeviceOffset device in devices)
                {
                    if (device.name == xrDevice.name)
                    {
                        Debug.Log("Controller Config Loaded");
                        return device;
                    }
                }


                Debug.Log("Controler offset not set for " + xrDevice.name + " using configuration for " + defaultDevice.name);
                return defaultDevice;
            }

            Debug.Log("No controllers found! using configuration for " + defaultDevice.name);
            return defaultDevice;
        }

    }
}

