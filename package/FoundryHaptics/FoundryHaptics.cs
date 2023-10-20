using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem;

namespace Foundry.Haptics
{
    public class FoundryHaptics : MonoBehaviour
    {
        public static void SendHapticToDevice(XRController device, float amplitude, float duration) 
        {
            var command = UnityEngine.InputSystem.XR.Haptics.SendHapticImpulseCommand.Create(0, amplitude, duration);
            device.ExecuteCommand(ref command);
        }
    }
}
