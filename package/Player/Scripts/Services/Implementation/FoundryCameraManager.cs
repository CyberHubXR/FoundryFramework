using UnityEngine;

namespace Foundry
{
    public class FoundryCameraManager : IFoundryCameraManager
    {
        private Camera _mainCamera;
        public Camera MainCamera
        {
            get => _mainCamera;
            set
            {
                if(!_mainCamera)
                    _mainCamera = value;
                else
                    Debug.LogError("Main camera already set!");
            }
        }
    }
}