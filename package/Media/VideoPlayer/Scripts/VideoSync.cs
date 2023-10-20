using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Fusion;
using UnityEngine.Video;

namespace Foundry
{

    /// <summary>
    /// Synchronizes a Unity <see cref="VideoPlayer"/> over the network.
    /// </summary>
    public class VideoSync : NetworkBehaviour
    {
        #region Member Variables
        private double videoStartTime;
        #endregion // Member Variables

        #region Unity Inspector Variables
        [SerializeField]
        [Tooltip("The video player to control. If not specified, the current GameObject will be searched.")]
        private VideoPlayer videoPlayer;
        #endregion // Unity Inspector Variables


        #region Private Methods

        /// <summary>
        /// Ensures all dependencies were met.
        /// </summary>
        /// <returns>
        /// <c>true</c> if all dependencies were met; otherwise <c>false</c>.
        /// </returns>
        private bool EnsureDependencies()
        {
            // If a player was not specified, try to find on current GameObject.
            if (videoPlayer == null)
            {
                videoPlayer = GetComponent<VideoPlayer>();
            }

            // If still not found, warn and disable.
            if (videoPlayer == null)
            {
                Debug.LogError($"{nameof(VideoSync)}: {nameof(VideoPlayer)} is not set. Disabling.");
                this.enabled = false;
                return false;
            }

            // All met
            return true;
        }

        /// <summary>
        /// Synchronizes network state to the player (if not currently authority).
        /// </summary>
        /// <returns>
        /// <c>true</c> if the current user is not authority and the player was updated; 
        /// otherwise <c>false</c>.
        /// </returns>
        private bool TryNetworkToPlayer()
        {
            // Ignore if we are authority
            if (this.HasStateAuthority) { return false; }

            // Make sure there's a clip
            if (videoPlayer.clip == null)
            {
                Debug.LogError($"{nameof(VideoSync)} '{name}' has no video clip. Ignoring network update.");
                return false;
            }

            // Make sure we can update the player
            if (!videoPlayer.canSetTime)
            {
                Debug.LogError($"{nameof(VideoSync)} '{name}' is unable to set time of the video player. Ignoring network update.");
                return false;
            }

            // Get the clip length
            double clipLength = videoPlayer.clip.length;

            // Get the current server time and subtract network start time
            // This tells us how long the video has been playing
            double startTime = Runner.Simulation.LatestServerState.Time - VideoStartTime;

            // If local start time is longer than the video itself, we need to account for loops
            if (startTime > clipLength)
            {
                // Figure out how many loops have occurred
                double loops = startTime / clipLength;

                // Drop the interger loops and keep only the decimal fraction of a loop
                double fractionalLoop = loops % 1;

                // Set local start time to the fraction of a loop
                startTime = videoPlayer.clip.length * fractionalLoop;
            }

            // Update player time
            videoPlayer.time = startTime;

            // Success!
            return true;
        }

        /// <summary>
        /// Synchronizes player state to the network (if current authority).
        /// </summary>
        /// <param name="isLoop">
        /// True if this should be considered a loop and ignore video player time.
        /// </param>
        /// <returns>
        /// <c>true</c> if the current user has authority and the network was updated; 
        /// otherwise <c>false</c>.
        /// </returns>
        private bool TryPlayerToNetwork(bool isLoop = false)
        {
            // Make sure we have authority
            if (!this.HasStateAuthority) { return false; }

            // Update network start time to be server time minus current video position
            // Essentially we're storing what NETWORK time the video would have been at zero
            if (isLoop)
            {
                // Ignore clip time and assume 0
                VideoStartTime = Runner.Simulation.LatestServerState.Time;
            }
            else
            {
                Debug.Log("Video Player: " + videoPlayer);
                Debug.Log("Runner Simulation: " + Runner.Simulation.LatestServerState);
                // Include clip time
                VideoStartTime = Runner.Simulation.LatestServerState.Time - videoPlayer.time;
            }
            
            // Success
            return true;
        }

        #endregion // Private Methods

        #region Event Handlers

        private void VideoPlayer_LoopPointReached(VideoPlayer source)
        {
            TryPlayerToNetwork(isLoop: true);
        }

        private void VideoPlayer_SeekCompleted(VideoPlayer source)
        {
            TryPlayerToNetwork();
        }

        private void VideoPlayer_Started(VideoPlayer source)
        {
            TryPlayerToNetwork();
        }

        #endregion // Event Handlers

        #region Unity Message Handlers

        /// <inheritdoc/>
        private void OnDisable() 
        {
            // Unsubscribe from player events
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached -= VideoPlayer_LoopPointReached;
                videoPlayer.seekCompleted -= VideoPlayer_SeekCompleted;
                videoPlayer.started -= VideoPlayer_Started;
            }
        }

        /// <inheritdoc/>
        private void OnEnable()
        {
            // Subscribe to player events
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached += VideoPlayer_LoopPointReached;
                videoPlayer.seekCompleted += VideoPlayer_SeekCompleted;
                videoPlayer.started += VideoPlayer_Started;
            }
        }

        /// <inheritdoc/>
        private void Awake()
        {
            // If we don't have dependencies, bail
            if (!EnsureDependencies()) { return; }
        }

        #endregion // Unity Message Handlers

        #region Photon Overrides
        /// <inheritdoc/>
        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                // We're  the authority. Update the network.
                TryPlayerToNetwork();
            }
            else
            {
                // We're not the authority. Update from network.
                TryNetworkToPlayer();
            }
        }
        #endregion // Photon Overrides

        /// <summary>
        /// Gets or sets the network conencted start time of the video.
        /// </summary>
        [Networked]
        public double VideoStartTime
        {
            get => videoStartTime;
            set
            {
                // Store
                videoStartTime = value;

                // Update from network (if not authroity)
                TryNetworkToPlayer();
            }
        }
    }
}