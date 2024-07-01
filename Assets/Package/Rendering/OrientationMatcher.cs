using System;
using CyberHub.Foundry;
using UnityEngine;

namespace Foundry
{
    /// <summary>
    /// Specifies which axis will be matched.
    /// </summary>
    [Flags]
    public enum OrientationMatchAxis
    {
        X = 1,
        Y = 2,
        Z = 4,
    };

    /// <summary>
    /// Orientates one object to match another.
    /// </summary>
    public class OrientationMatcher : MonoBehaviour
    {
        #region Unity Inspector Variables

        [SerializeField]
        [Tooltip("Which axis will be matched with the target.")]
        private OrientationMatchAxis axis = (OrientationMatchAxis.X | OrientationMatchAxis.Y | OrientationMatchAxis.Z);

        [SerializeField]
        [Tooltip("Should Match be called on Enable?")]
        private bool matchOnEnable = true;

        [SerializeField]
        [Tooltip("Should Match be called on Start?")]
        private bool matchOnStart = true;

        [SerializeField]
        [Tooltip("Should Match be called every Update?")]
        private bool matchOnUpdate = false;

        [SerializeField]
        [Tooltip("The source transform to update. If not specified, the current GameObject will be used.")]
        private Transform source;

        [SerializeField]
        [Tooltip("The transform to match.")]
        private Transform target;

        [SerializeField]
        [Tooltip("If true, target will be set to the main camera whenever the behavior is enabled.")]
        private bool targetMainCamera = false;

        #endregion Unity Inspector Variables

        #region Private Methods

        /// <inheritdoc />
        private void OnEnable()
        {
            // If we have no source, use ourselves
            if (source == null) { source = transform; }

            // If we're using the main camera, override target
            if (targetMainCamera)
            {
                TargetMainCamera();
            }

            // Match?
            if (matchOnEnable) { Match(); }
        }

        /// <inheritdoc />
        private void Start()
        {
            if (matchOnStart) { Match(); }
        }

        /// <summary>
        /// Sets the target to the main camera.
        /// </summary>
        private void TargetMainCamera()
        {
            // Attempt to get Foundry main camera
            
            Camera mainCamera;
            if (FoundryApp.TryGetService(out IFoundryCameraManager manager))
                mainCamera = manager.MainCamera;
            else
                mainCamera = Camera.main;

            // If neither found, warn
            if (mainCamera == null)
            {
                Debug.LogWarning($"{nameof(OrientationMatcher)} {nameof(targetMainCamera)} is true, but no main camera could be found.");
                return;
            }

            // Set the target
            target = mainCamera.transform;
        }

        /// <inheritdoc />
        private void Update()
        {
            if (matchOnUpdate) { Match(); }
        }

        #endregion Private Methods

        #region Public Methods

        /// <summary>
        /// Matches the source to the target
        /// </summary>
        public void Match()
        {
            // Match only specified axis
            source.rotation = Quaternion.Euler(
                (axis.HasFlag(OrientationMatchAxis.X) ? target.rotation.eulerAngles.x : 0f),
                (axis.HasFlag(OrientationMatchAxis.Y) ? target.rotation.eulerAngles.y : 0f),
                (axis.HasFlag(OrientationMatchAxis.Z) ? target.rotation.eulerAngles.z : 0f));
        }

        #endregion Public Methods
    }
}