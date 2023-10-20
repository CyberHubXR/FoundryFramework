using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    public class PlacePointSoundEffects : MonoBehaviour
    {
        public PlacePoint placePoint;
        public AudioSource audioSource;
        public AudioClip highlightSound;
        public AudioClip unhighlightSound;
        public AudioClip placeSound;
        public AudioClip removeSound;

        void OnEnable() {
            if(audioSource == null)
                audioSource = GetComponent<AudioSource>();

            placePoint.OnHighlight.AddListener(OnHighlight);
            placePoint.OnStopHighlight.AddListener(OnUnhighlight);
            placePoint.OnPlace.AddListener(OnPlace);
            placePoint.OnRemove.AddListener(OnRemove);
        }

        void OnDisable() {
            placePoint.OnHighlight.RemoveListener(OnHighlight);
            placePoint.OnStopHighlight.RemoveListener(OnUnhighlight);
            placePoint.OnPlace.RemoveListener(OnPlace);
            placePoint.OnRemove.RemoveListener(OnRemove);
        }

        void OnHighlight(PlacePoint placePoint, SpatialGrabbable grabbable) {
            if (highlightSound != null) 
                audioSource.PlayOneShot(highlightSound);
        }


        void OnUnhighlight(PlacePoint placePoint, SpatialGrabbable grabbable) {
            if (unhighlightSound != null) 
                audioSource.PlayOneShot(unhighlightSound);
        }

        void OnPlace(PlacePoint placePoint, SpatialGrabbable grabbable) {
            if (placeSound != null) 
                audioSource.PlayOneShot(placeSound);
        }

        void OnRemove(PlacePoint placePoint, SpatialGrabbable grabbable) {
            if (removeSound != null) 
                audioSource.PlayOneShot(removeSound);
        }
    }
}
