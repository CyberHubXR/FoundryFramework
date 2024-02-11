using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    public class SpatialLookSoundEffects : MonoBehaviour
    {
        public SpatialLookable spatialLookable;
        public AudioSource audioSource;
        public AudioClip lookStartSound;
        public AudioClip lookStopSound;
        public AudioClip lookStaySound;

        private void OnEnable() {
            if(audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if(spatialLookable == null)
                spatialLookable = GetComponent<SpatialLookable>();

            spatialLookable.OnStartLook.AddListener(OnLookStart);
            spatialLookable.OnStopLook.AddListener(OnLookStop);
            spatialLookable.OnLookStay.AddListener(OnLookStay);
        }

        void OnLookStart(SpatialLook spatialLook, SpatialLookable spatialLookable) {
            audioSource.PlayOneShot(lookStartSound);
        }

        void OnLookStop(SpatialLook spatialLook, SpatialLookable spatialLookable) {
            audioSource.PlayOneShot(lookStopSound);
        }

        void OnLookStay(SpatialLook spatialLook, SpatialLookable spatialLookable) {
            audioSource.PlayOneShot(lookStaySound);
        }
    }
}
