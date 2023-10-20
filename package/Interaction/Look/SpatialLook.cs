using System;
using System.Collections;
using UnityEngine;

namespace Foundry
{
    public class SpatialLook : MonoBehaviour
    {
        [Serializable]
        public struct CastWidth {
            [Tooltip("The distance from the player to the start of this spherecast.")]
            public float distance;
            [Tooltip("The radius of the spherecast starting at this distance.")]
            public float radius;

            public CastWidth(float distance, float radius) {
                this.distance = distance;
                this.radius = radius;
            }
        }
        
        [Tooltip("The width of the player view spherecast at different distances.")]
        public CastWidth[] ViewWidth = new CastWidth[]{new (0, 0.1f), new (1.2f, 1), new(3, 2f)};

        public float maxDistance = 5000f;
        public LayerMask layerMask;
        
        [Tooltip("How often to execute our raycasts. A higher rate will be more responsive but more expensive.")]
        [Range(5, 60)]
        public int tickRate = 20;

        public SpatialLookable currentLookable { get; private set; }

        private Coroutine checkLookRoutine;
        private void OnEnable()
        {
            checkLookRoutine = StartCoroutine(CheckLook());
        }

        private void OnDisable()
        {
            StopCoroutine(checkLookRoutine);
        }

        protected virtual IEnumerator CheckLook() {
            while (enabled)
            {
                Vector3 forward = transform.forward;
                Vector3 start = transform.position;
            
                bool lookableFound = false;
                for (int i = 0; i < ViewWidth.Length; i++)
                {
                    // Spherecast to the position of the next spherecast, or the max distance if there is no next spherecast
                    float length = i + 1 < ViewWidth.Length ? ViewWidth[i + 1].distance  - ViewWidth[i].distance : maxDistance - ViewWidth[i].distance;
                
                    //We subtract radius from start since the spherecast starts at the edge of the sphere and wont detect colliders it's intersecting at start, so otherwise we'd get gaps between the spherecasts
                    Vector3 rayStart = start + forward * (ViewWidth[i].distance - ViewWidth[i].radius - 0.002f);

                    if (Physics.SphereCast(new Ray(rayStart, forward), ViewWidth[i].radius, out RaycastHit hit, length, layerMask)) {
               
                        if (hit.transform != null && hit.transform.TryGetComponent(out SpatialLookable tempLookable)) {
                            if(currentLookable != null) {
                                if(currentLookable != tempLookable) {
                                    StopLook(currentLookable);
                                    StartLook(tempLookable);
                                }
                            }
                            else{
                                StartLook(tempLookable);
                            }
                        }
                        else if (currentLookable != null)
                        {
                            StopLook(currentLookable);
                        }

                        lookableFound = true;
                        break;
                    }
                }

                if (!lookableFound && currentLookable)
                    StopLook(currentLookable);
                yield return new WaitForSeconds(1f / tickRate);
            }
        }

        protected virtual void StartLook(SpatialLookable lookable) {
            if(lookable != currentLookable) {
                currentLookable = lookable;
                lookable.StartLook(this);
            }
        }

        protected virtual void StopLook(SpatialLookable lookable) {
            if(lookable == currentLookable) {
                lookable.StopLook(this);
                currentLookable = null;
            }
        }

        private void OnValidate()
        {
            // Make sure that a distance is never smaller than the previous distance, as that would break our logic
            float lastDistance = 0;
            for (int i = 0; i < ViewWidth.Length; i++)
            {
                ViewWidth[i].distance = Mathf.Max(lastDistance, ViewWidth[i].distance);
                lastDistance = ViewWidth[i].distance;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            
            Vector3 forward = transform.forward;
            Vector3 start = transform.position;
            for(int i = 0; i < ViewWidth.Length; i++)
                Gizmos.DrawWireSphere(start + forward * ViewWidth[i].distance, ViewWidth[i].radius);
            Gizmos.DrawLine(start, start + forward * maxDistance);
        }
    }
}
