using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    public class TeleportRaycasterSounds : MonoBehaviour
    {
        public TeleportRaycaster raycaster;
        public AudioSource audioSource;
        public AudioClip validTeleportSound;
        public AudioClip invalidTeleportSound;
        public AudioClip showTeleportSound;
        public AudioClip hideTeleportSound;

        private void OnEnable() {
            if(raycaster == null)
                raycaster = GetComponent<TeleportRaycaster>();

            if(audioSource == null)
                audioSource = GetComponent<AudioSource>();

            raycaster.OnInvalidTeleport.AddListener(PlayInvalidTeleportSound);
            raycaster.OnValidTeleport.AddListener(PlayValidTeleportSound);
            raycaster.ShowTeleport.AddListener(PlayShowTeleportSound);
            raycaster.HideTeleport.AddListener(PlayHideTeleportSound);
        }

        private void OnDisable() {
            raycaster.OnInvalidTeleport.RemoveListener(PlayInvalidTeleportSound);
            raycaster.OnValidTeleport.RemoveListener(PlayValidTeleportSound);
            raycaster.ShowTeleport.RemoveListener(PlayShowTeleportSound);
            raycaster.HideTeleport.RemoveListener(PlayHideTeleportSound);
        }

        void PlayInvalidTeleportSound(TeleportRaycaster raycaster) {
            if(audioSource != null && invalidTeleportSound != null)
                audioSource.PlayOneShot(invalidTeleportSound);
        }

        void PlayValidTeleportSound(TeleportRaycaster raycaster) {
            if(audioSource != null && validTeleportSound != null)
                audioSource.PlayOneShot(validTeleportSound);
        }

        void PlayShowTeleportSound(TeleportRaycaster raycaster) {
            if(audioSource != null && showTeleportSound != null)
                audioSource.PlayOneShot(showTeleportSound);
        }

        void PlayHideTeleportSound(TeleportRaycaster raycaster) {
            if(audioSource != null && hideTeleportSound != null)
                audioSource.PlayOneShot(hideTeleportSound);
        }




    }
}
