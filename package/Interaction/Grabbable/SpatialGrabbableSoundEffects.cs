using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    public class SpatialGrabbableSoundEffects : MonoBehaviour
    {

        public SpatialGrabbable spatialGrabbable;
        public float randomPitchRange = 0.1f;
        public AudioSource audioSource;
        public AudioClip highlightSound;
        public AudioClip unhighlightSound;
        public AudioClip onGrabSound;
        public AudioClip onReleaseSound;

        float startPitch;
        private void OnEnable() {
            if(audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if(spatialGrabbable == null)
                spatialGrabbable = GetComponent<SpatialGrabbable>();

            spatialGrabbable.OnAnyHighlightEvent.AddListener(OnHighlight);
            spatialGrabbable.OnAnyStopHighlightEvent.AddListener(OnUnhighlight);
            spatialGrabbable.OnGrabEvent.AddListener(OnGrab);
            spatialGrabbable.OnReleaseEvent.AddListener(OnRelease);

            startPitch = audioSource.pitch;
        }

        void OnHighlight(SpatialHand hand, SpatialGrabbable grabbable) {
            if(audioSource != null && highlightSound != null) { 
                audioSource.pitch = startPitch + Random.Range(-randomPitchRange, randomPitchRange);
                audioSource.PlayOneShot(highlightSound);
            }
        }

        void OnUnhighlight(SpatialHand hand, SpatialGrabbable grabbable) {
            if(audioSource != null && unhighlightSound != null) {
                audioSource.pitch = startPitch + Random.Range(-randomPitchRange, randomPitchRange);
                audioSource.PlayOneShot(unhighlightSound);
            }
        }

        void OnGrab(SpatialHand hand, SpatialGrabbable grabbable) {
            if(audioSource != null && onGrabSound != null) {
                audioSource.pitch = startPitch + Random.Range(-randomPitchRange, randomPitchRange);
                audioSource.PlayOneShot(onGrabSound);
            }
        }

        void OnRelease(SpatialHand hand, SpatialGrabbable grabbable) {
            if(audioSource != null && onReleaseSound != null) {
                audioSource.pitch = startPitch + Random.Range(-randomPitchRange, randomPitchRange);
                audioSource.PlayOneShot(onReleaseSound);
            }
        }

    }
}
