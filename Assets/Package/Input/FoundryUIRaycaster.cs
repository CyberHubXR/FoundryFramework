using System;
using Foundry.Networking;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Foundry
{
    public class FoundryUIRaycaster : NetworkComponent
    {
        public enum InputSource
        {
            Desktop,
            Left,
            Right
        }
        public InputSource inputSource;
        public bool alwaysShow = false;
        
        private InputAction inputAction;
        private LineRenderer _renderer;
        private Quaternion localOffset;

        [SerializeField]
        float minRayLength = 0.05f;

        [SerializeField]
        float maxRayLength = 5f;

        [SerializeField]
        float rayStartOffset = 0.02f;


        public override void OnConnected()
        {
            if(!IsOwner)
                gameObject.SetActive(false);
        }
        
        void Start()
        {
            switch(inputSource)
            {
                case InputSource.Desktop:
                    inputAction = SpatialInputManager.instance.uiClickDesktop.action;
                    break;
                case InputSource.Left:
                    inputAction = SpatialInputManager.instance.uiClickLeftXR.action;
                    break;
                case InputSource.Right:
                    inputAction = SpatialInputManager.instance.uiClickRightXR.action;
                    break;
            }
            _renderer = GetComponent<LineRenderer>();
            localOffset = transform.localRotation;
        }

        public bool IsClicked()
        {
            return inputAction.ReadValue<float>() > 0.5f;
        }

        public bool IsActive()
        {
            return FoundryUIInput.activeRaycaster == this;
        }

        void Update()
        {
            if(IsClicked() && !IsActive())
                FoundryUIInput.SetActiveRaycaster(this);
            transform.rotation = transform.parent.rotation * localOffset;
        }

        private void LateUpdate()
        {
            if (!_renderer) return;

            float rayLength = maxRayLength;

            // Always render at least a small ray
            float clampedLength = Mathf.Max(minRayLength, rayLength);
            Vector3 start = transform.position + transform.forward * rayStartOffset;

            _renderer.enabled = IsActive() || alwaysShow;
            _renderer.SetPosition(0, start);
            _renderer.SetPosition(1, start + transform.forward * clampedLength);
        }

    }
}