using System;
using System.Collections;
using System.Collections.Generic;
using CyberHub.Foundry;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Foundry
{
    public class FoundryUIInput : BaseInput
    {
        public static FoundryUIInput current;
        private FoundryUIRaycaster currentRaycaster;

        [Tooltip(
            "Graphic raycasters require a camera for reference, set this to reference the camera that world space canvases are set to reference. Make sure you do not care about it's location")]
        public Camera canvasCamera;

        private bool isPressed = false;
        private bool wasPressed = false;

        public override bool mousePresent
        {
            get { return true; }
        }

        public override bool touchSupported
        {
            get { return false; }
        }

        public override bool GetMouseButtonDown(int button)
        {
            if (button != 0)
                return false;
            return isPressed && !wasPressed;
        }

        /// <summary>
        /// Interface to Input.GetMouseButtonUp. Can be overridden to provide custom input instead of using the Input class.
        /// </summary>
        public override bool GetMouseButtonUp(int button)
        {
            if (button != 0)
                return false;
            return !isPressed && wasPressed;
        }

        public override bool GetMouseButton(int button)
        {
            if (button != 0)
                return false;
            return isPressed;
        }

        public override Vector2 mousePosition
        {
            get
            {
                if(currentRaycaster)
                    return canvasCamera.ViewportToScreenPoint(new Vector2(0.5f, 0.5f));
                return SpatialInputManager.instance.mousePos.action.ReadValue<Vector2>();
            }
        }

        public override float GetAxisRaw(string axisName)
        {
            //TODO return scrolling data
            return 0;
        }

        public static void SetActiveRaycaster(FoundryUIRaycaster caster)
        {
            if (!current)
                return;
            // Do not set if the current raycaster is still pressed
            if (current.currentRaycaster?.IsClicked() ?? false)
                return;
            current.currentRaycaster = caster;
            current.isPressed = false;
        }
        
        public static FoundryUIRaycaster activeRaycaster => current.currentRaycaster;

        void Awake()
        {
            current = this;
            gameObject.AddComponent<StandaloneInputModule>().inputOverride = this;
        }

        private void Update()
        {
            if (currentRaycaster == null)
            {
                var mainCamera = FoundryApp.GetService<IFoundryCameraManager>().MainCamera;
                canvasCamera.transform.position = mainCamera.transform.position;
                canvasCamera.transform.rotation = mainCamera.transform.rotation;
                
                wasPressed = isPressed;
                isPressed = SpatialInputManager.instance.uiClickDesktop.action.ReadValue<float>() > 0.5f;
                return;
            }

            canvasCamera.transform.position = currentRaycaster.transform.position;
            canvasCamera.transform.rotation = currentRaycaster.transform.rotation;
            wasPressed = isPressed;
            isPressed = currentRaycaster.IsClicked();

        }
    }
}