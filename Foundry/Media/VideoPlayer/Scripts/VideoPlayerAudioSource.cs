using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace Foundry
{
    /// <summary>
    /// Causes a <see cref="VideoPlayer"/> to use an <see cref="AudioSource"/> for one or more tracks.
    /// </summary>
    public class VideoPlayerAudioSource : MonoBehaviour
    {
        #region Unity Inspector Variables
        [SerializeField]
        [Tooltip("The VideoPlayer that will be linked to the AudioSource. If not specified, the current GameObject will be searched.")]
        private VideoPlayer videoPlayer;

        [SerializeField]
        [Tooltip("The AudioSource that will be linked to the VideoPlayer. If not specified, the current GameObject will be searched.")]
        private AudioSource audioSource;

        [SerializeField]
        [Tooltip("The audio track that will be linked to the AudioSource.")]
        private ushort audioTrack = 0;
        #endregion // Unity Inspector Variables

        #region Private Methods
        /// <summary>
        /// Connects the <see cref="AudioSource"/> to the <see cref="VideoPlayer"/>.
        /// </summary>
        private void ConnectSourceToPlayer()
        {
            // Make sure player is using AudioSource mode
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;

            // Make sure the specified track is enabled
            videoPlayer.EnableAudioTrack(audioTrack, true);

            // Make sure the track is routed to the audio source
            videoPlayer.SetTargetAudioSource(audioTrack, audioSource);
        }

        /// <summary>
        /// Attempts to ensures that dependent components can be resolved.
        /// </summary>
        private bool TryResolveDependencies()
        {
            // If the video player wasn't specified, try to find it on the current GameObject
            if (videoPlayer == null)
            {
                videoPlayer = GetComponent<VideoPlayer>();
            }

            // If the audio source wasn't specified, try to find it on the current GameObject
            if (videoPlayer == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            // Make sure all validated
            if ((videoPlayer == null) || (audioSource == null))
            {
                Debug.LogError($"{nameof(VideoPlayerAudioSource)}: Both {nameof(VideoPlayer)} and {nameof(AudioSource)} must be specified. Disabling.");
                this.enabled = false;
                return false;
            }

            // Success
            return true;
        }
        #endregion // Private Methods

        #region Unity Message Handlers
        /// <summary>
        /// Start is called before the first frame update
        /// </summary>
        private void Start()
        {
            // Try and resolve dependencies
            if (TryResolveDependencies())
            {
                // Resolved. Connect.
                ConnectSourceToPlayer();
            }
        }
        #endregion // Unity Message Handlers
    }

}