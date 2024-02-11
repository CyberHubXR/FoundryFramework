using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    public class SpatialTouchableSoundEffects : MonoBehaviour
    {
        public SpatialTouchable spatialTouchable;
        public AudioSource audioSource;
        public AudioClip touchStartSound;
        public AudioClip touchStopSound;
        public AudioClip touchStaySound;

        private void OnEnable() {
            if(audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if(spatialTouchable == null)
                spatialTouchable = GetComponent<SpatialTouchable>();

            spatialTouchable.OnStartTouch.AddListener(OnTouchStart);
            spatialTouchable.OnStopTouch.AddListener(OnTouchStop);
            spatialTouchable.OnTouchStay.AddListener(OnTouchStay);
        }

        void OnTouchStart(NetEventSource src, bool b) {
            audioSource.PlayOneShot(touchStartSound);
        }

        void OnTouchStop(NetEventSource src, bool b) {
            audioSource.PlayOneShot(touchStopSound);
        }

        void OnTouchStay(NetEventSource src, bool b) {
            audioSource.PlayOneShot(touchStaySound);
        }
    }
}
