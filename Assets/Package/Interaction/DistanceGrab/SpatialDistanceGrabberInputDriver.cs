using Foundry;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpatialDistanceGrabberInputDriver : MonoBehaviour
{
    public SpatialDistanceGrabber grabber;
    public InputActionProperty pointInput;
    public InputActionProperty selectionInput;

    bool pointing = false;
    bool selecting = false;

    private void OnEnable() {
        pointInput.action.Enable();
        selectionInput.action.Enable();
    }

}
