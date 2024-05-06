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
        public float smoothing = 0.3f;
        
        private InputAction inputAction;
        private LineRenderer _renderer;
        private LayerMask _uiLayers;
        private Quaternion localOffset;
        private Quaternion lastRotation;

        public override void OnConnected()
        {
            if(!IsOwner)
                gameObject.SetActive(false);
        }
        
        void Start()
        {
            _uiLayers = LayerMask.GetMask("UI");
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
            lastRotation = transform.rotation;
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
            transform.rotation = Quaternion.Slerp(transform.parent.rotation * localOffset, lastRotation, smoothing);
            lastRotation = transform.rotation;

        }

        private void LateUpdate()
        {
            if (_renderer)
            {
                bool uiHit = Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 10, _uiLayers);
                _renderer.enabled = IsActive() && (uiHit || alwaysShow);
                if (!uiHit && alwaysShow)
                    hit.point = transform.position + transform.forward * 10;
                _renderer.SetPosition(0, transform.position);
                _renderer.SetPosition(1, hit.point);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Debug.DrawRay(transform.position, transform.forward * 10, Color.red);
        }
    }
}