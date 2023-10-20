using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Foundry.SpatialAudio
{
    /// <summary>
    /// Provides settings for a spatial audio source regardless of the spatial audio provider used.
    /// </summary>
    /// <remarks>
    /// TODO: This class is not yet fully implemented.
    /// </remarks>
    public class SpatialAudioSource : MonoBehaviour
    {
        #region Unity Inspector Variables
        [SerializeField]
        [Tooltip("Whether the audio source has directionality.")]
        private bool directivity = true;

        [SerializeField]
        [Tooltip("Whether the audio source is attenuated as the distance to the camera increases.")]
        private bool distanceAttenuation = true;


        [SerializeField]
        [Tooltip("Raised whenever Directivity has changed.")]
        private UnityEvent directivityChanged = new UnityEvent();

        [SerializeField]
        [Tooltip("Raised whenever Distance Attenuation has changed.")]
        private UnityEvent distanceAttenuationChanged = new UnityEvent();
        #endregion // Unity Inspector Variables 

        #region Public Properties
        /// <summary>
        /// Gets or sets whether the audio source has directionality.
        /// </summary>
        public bool Directivity { get => directivity; set => directivity = value; }

        /// <summary>
        /// Gets or sets whether the audio source is attenuated as the distance to the camera increases.
        /// </summary>
        public bool DistanceAttenuation { get => distanceAttenuation; set => distanceAttenuation = value; }
        #endregion // Public Properties

        #region Unity Events
        /// <summary>
        /// Raised whenever <see cref="Directivity"/> has changed.
        /// </summary>
        public UnityEvent DirectivityChanged => directivityChanged;

        /// <summary>
        /// Raised whenever <see cref="DistanceAttenuation"/> has changed.
        /// </summary>
        public UnityEvent DistanceAttenuationChanged => distanceAttenuationChanged;

        #endregion // Unity Events    
    }
}