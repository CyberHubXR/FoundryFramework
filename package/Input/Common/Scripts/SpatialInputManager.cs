using System;
using System.Collections;
using System.Collections.Generic;
using Foundry;
using UnityEngine;

using UnityEngine.InputSystem;

public class SpatialInputManager : MonoBehaviour
{

    public static SpatialInputManager instance;

    [Header("Desktop Input")] 
    [Range(0.1f, 3f)]
    public float lookSpeed = 0.4f;
    public InputActionProperty movementDesktop;
    public InputActionProperty lookDesktop;
    public InputActionProperty uiClickDesktop;
    public InputActionProperty mousePos;
    [Space, Space, Space, Space]
    public InputActionProperty grabDesktop;
    public InputActionProperty reachDesktop;
    public InputActionProperty sprintDesktop;
    public InputActionProperty toggleUIDesktop;

    [Space, Space, Space, Space]
    [Header("XR Input")]
    public InputActionProperty movementXR;
    public InputActionProperty sprintXR;
    public InputActionProperty turnXR;
    [Space, Space, Space, Space]
    public InputActionProperty grabRightXR;
    public InputActionProperty grabLeftXR;
    public InputActionProperty toggleRightPointerXR;
    public InputActionProperty toggleLeftPointerXR;
    [Space, Space, Space, Space]
    public InputActionProperty toggleUIXR;
    public InputActionProperty uiClickLeftXR;
    public InputActionProperty uiClickRightXR;
    [Space, Space, Space, Space]
    public InputActionProperty resetExperience;
    [Space, Space, Space, Space]
    public InputActionProperty handVelocityR;
    public InputActionProperty handVelocityL;

    


    public enum MovementReference
    {
        Head,
        LeftHand,
        RightHand
    };
    public MovementReference movementReference;
    public float snapTurnAngle = 45;

    TrackingMode activeMode;

    private void Awake()
    {
        if (instance != null)
            enabled = false;
        else 
            instance = this;
    }

    private void ActivateActionsInternal(TrackingMode mode)
    {
        if (mode == TrackingMode.OnePoint)
        {
            movementDesktop.action.Enable();
            grabDesktop.action.Enable();
            reachDesktop.action.Enable();
            lookDesktop.action.Enable();
            mousePos.action.Enable();
            uiClickDesktop.action.Enable();
            
            resetExperience.action.Enable();
            sprintDesktop.action.Enable();
            toggleUIDesktop.action.Enable();
        }
        else
        {
            movementXR.action.Enable();
            turnXR.action.Enable();
            grabLeftXR.action.Enable();
            grabRightXR.action.Enable();
            uiClickLeftXR.action.Enable();
            uiClickRightXR.action.Enable();
            handVelocityL.action.Enable();
            handVelocityR.action.Enable();
            
            resetExperience.action.Enable();
            sprintXR.action.Enable();
            toggleUIXR.action.Enable();
            toggleRightPointerXR.action.Enable();
            toggleLeftPointerXR.action.Enable();

        }
        activeMode = mode;
    }

    public static void ActivateActions(TrackingMode mode)
    {
        instance.ActivateActionsInternal(mode);
    }

    public static Vector2 movementInput
    {
        get
        {
            if (instance.activeMode == TrackingMode.OnePoint)
                return instance.movementDesktop.action.ReadValue<Vector2>();
            else
                return instance.movementXR.action.ReadValue<Vector2>();
        }
    }
}
