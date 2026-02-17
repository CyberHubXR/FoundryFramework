using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Foundry
{
    public class PlayerMenuSpawner : MonoBehaviour
    {
        public enum InputSource
        {
            Desktop,
            XR
        }

        public InputSource inputSource;
        public Transform head;
        public Transform spawnOffset;

        private InputAction action;
        void Start()
        {
            if(inputSource == InputSource.Desktop)
                action = SpatialInputManager.instance.toggleUIDesktop.action;
            else
                action = SpatialInputManager.instance.toggleUIXR.action;
            
            action.performed += OnInputPerformed;
        }

        void OnInputPerformed(InputAction.CallbackContext context)
        {
            SpawnMenu();
        }

        public void SpawnMenu ()
        {
            PlayerMenuAnimator.Instance?.ToggleMenu(spawnOffset, head);
        }
    }
}
