using System;
using CyberHub.Brane;
using Foundry.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Foundry
{
    public class LoadingScreenManager : MonoBehaviour
    {
        #region Private Fields

        private LayerMask originalRenderMask;
        private IPlayerRigManager playerRigManager;
        private ISceneNavigator sceneNavigator;
        private int uncompletedLoadingPhases = 0;
        private static LoadingScreenManager instance;

        #endregion Private Fields

        #region Unity Inspector Variables
        [SerializeField]
        [Tooltip("The GameObject containing the visuals for the loading screen. These will be shown during loading and hidden when loaded.")]
        private GameObject visuals;

        [SerializeField]
        [Tooltip("The UI Image object that will be used to display progress.")]
        private Image progressImage;
        
        
        [SerializeField]
        [Tooltip("The UI Image object that will be used to display errors.")]
        private TMP_Text errorText;
        
        #endregion Unity Inspector Variables

        #region Private Methods

        /// <summary>
        /// Hides the loading visuals
        /// </summary>
        private void HideVisuals()
        {
            // Verify visuals
            if (!VerifyVisuals()) { return; }

            // Hide (TODO: Fade)
            visuals.SetActive(false);
        }

        /// <summary>
        /// Reparents the loading content to the specified transform.
        /// </summary>
        /// <param name="parent">
        /// The transform that should be the parent.
        /// </param>
        private void SetLoadScreenParent(Transform parent)
        {
            // Verify visuals
            if (!VerifyVisuals()) { return; }

            // Reparent
            visuals.transform.SetParent(parent, worldPositionStays: false);

            // Reset position and direction
            visuals.transform.localPosition = Vector3.zero;
            visuals.transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Shows the loading visuals
        /// </summary>
        private void ShowVisuals()
        {
            // Verify visuals
            if (!VerifyVisuals()) { return; }

            // Show (TODO: Fade)
            visuals.SetActive(true);
        }

        /// <summary>
        /// Verifies that we have visuals to show.
        /// </summary>
        /// <c>true</c> if there are visuals to show; otherwise <c>false</c>.
        private bool VerifyVisuals()
        {
            // If we don't have visuals, warn
            if (visuals == null)
            {
                Debug.LogWarning($"{nameof(LoadingScreenManager)}: {nameof(visuals)} have not been provided.");
                return false;
            }
            
            var visualsLayer = LayerMask.NameToLayer("LoadingVisuals");
            
            foreach(var t in visuals.GetComponentsInChildren<Transform>())
            {
                t.gameObject.layer = visualsLayer;
            }

            // Verified
            return true;
        }

        /// <summary>
        /// Loading being finished is dependant on multiple events, all events call this method and loading is finished
        /// when all calls are complete
        /// </summary>
        private void CompleteLoadingPhase()
        {
            if (--uncompletedLoadingPhases != 0)
                return;
            HideVisuals();
            // Get the main camera
            var mainCamera = BraneApp.GetService<IFoundryCameraManager>().MainCamera;

            // Set the camera back to the original render mask, showing all content
            mainCamera.cullingMask = originalRenderMask;
        }

        #endregion Private Methods

        #region Unity Message Handlers

        /// <inheritdoc/>
        private void OnDisable()
        {
            if(instance == this)
                instance = null;
            
            // Unsubscribe from events
            sceneNavigator.NavigationCompleted -= SceneNavigator_NavigationCompleted;
            sceneNavigator.NavigationStarting -= SceneNavigator_NavigationStarting;
            sceneNavigator.ProgressChanged -= SceneNavigator_ProgressChanged;
            playerRigManager.PlayerRigCreated -= PlayerRigManager_PlayerRigCreated;
            playerRigManager.PlayerRigBorrowed -= PlayerRigManager_PlayerRigBorrowed;
        }

        /// <inheritdoc/>
        private void OnEnable()
        {
            if (instance != null && instance != this)
                return;
            instance = this;
            
            // Get services
            sceneNavigator = BraneApp.GetService<ISceneNavigator>();
            playerRigManager = BraneApp.GetService<IPlayerRigManager>();

            // Subscribe to events
            sceneNavigator.NavigationCompleted += SceneNavigator_NavigationCompleted;
            sceneNavigator.NavigationStarting += SceneNavigator_NavigationStarting;
            sceneNavigator.ProgressChanged += SceneNavigator_ProgressChanged;
            playerRigManager.PlayerRigCreated += PlayerRigManager_PlayerRigCreated;
            playerRigManager.PlayerRigBorrowed += PlayerRigManager_PlayerRigBorrowed;

            // If we already have a rig, reparent the visuals now
            if (playerRigManager.Rig != null)
            {
                SetLoadScreenParent(playerRigManager.Rig.transform);
            }

            // Visuals will be active based on whether a navigation is currently in progress
            visuals.SetActive(sceneNavigator.IsNavigating);
        }

        #endregion Unity Message Handlers

        #region  Static Methods

        public static void FailLoad(string reason)
        {
            instance.errorText.text = reason;
        }

        #endregion

        #region Event Handlers

        private void PlayerRigManager_PlayerRigCreated(IPlayerControlRig rig)
        {
            // Reparent visuals
            SetLoadScreenParent(rig.transform);
        }

        /// <summary>
        /// Loading is only truly complete once a player script has taken ownership of the player rig.
        /// </summary>
        private void PlayerRigManager_PlayerRigBorrowed(IPlayerControlRig rig)
        {
            CompleteLoadingPhase();
        }

        private void SceneNavigator_NavigationCompleted(ISceneNavigationEntry scene)
        {
            CompleteLoadingPhase();
        }

        private void SceneNavigator_NavigationStarting(ISceneNavigationEntry scene)
        {
            // Get the main camera
            var mainCamera = BraneApp.GetService<IFoundryCameraManager>().MainCamera;

            // Store the original render mask
            originalRenderMask = mainCamera.cullingMask;

            // Tell the camera to render ONLY loading visuals
            mainCamera.cullingMask = 1 << LayerMask.NameToLayer("LoadingVisuals");

            // Show the loading visuals
            ShowVisuals();
            // Phase 1: Scene loading, Phase 2: Player instantiation (delayed by a fair bit on networked scenes)
            uncompletedLoadingPhases = Math.Max(2, uncompletedLoadingPhases + 2);
        }

        private void SceneNavigator_ProgressChanged(object sender, ProgressReport e)
        {
            // If we don't have an image control for progress, nothing to do
            if (progressImage == null) { return; }

            // Update the fill
            progressImage.fillAmount = e.Progress;
        }
        #endregion Event Handlers
    }
}