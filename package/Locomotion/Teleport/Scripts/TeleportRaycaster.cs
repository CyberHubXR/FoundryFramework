using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Foundry
{
    public class TeleportRaycaster : MonoBehaviour
    {

        [Header("Teleport settings")]
        public bool teleportPointOnly = false;
        [Tooltip("Layers that block the raycast")]
        public LayerMask cancelTeleportLayers = ~0;
        [Tooltip("Layers that can be teleported too")]
        public LayerMask teleportLayers;
        [Space]
        public Vector3 velocity = Vector3.forward;
        public Vector3 gravity = Vector3.down * 9.8f;
        [Space]
        public float step = 0.5f;
        public float maxRange = 10f;
        public float maxAngle = 45f;

        [Header("Preview settings")]
        public GameObject reticle;
        public LineRenderer line;
        public bool autoToggleLine = true;
        public Gradient validColor;
        public Gradient invalidColor;
        public QueryTriggerInteraction queryTriggers = QueryTriggerInteraction.Ignore;

        [Header("Events")]
        public UnityEvent<TeleportRaycaster> ShowTeleport;
        public UnityEvent<TeleportRaycaster> HideTeleport;
        public UnityEvent<TeleportRaycaster> OnValidTeleport;
        public UnityEvent<TeleportRaycaster> OnInvalidTeleport;

        Vector3 teleportPos;
        bool validTeleport;
        bool raycasting;

        Collider[] collidersNonAlloc = new Collider[64];
        TeleportPoint currentTeleportPoint;
        TeleportPoint[] teleportPoints;




        private void OnEnable() {
            teleportPoints = FindObjectsOfType<TeleportPoint>();
            ToggleTeleportPoints(false);
            line = GetComponent<LineRenderer>();
            line.enabled = false;
        }

        void LateUpdate(){
            if(raycasting)
                UpdateRaycaster();
        }


        void ToggleTeleportPoints(bool enabled) {
            foreach(var teleportPoint in teleportPoints) {
                if(!teleportPoint.alwaysShow || enabled)
                    teleportPoint.gameObject.SetActive(enabled);
            }
        }

        /// <summary>Activates the first stage of teleportation, shows the raycaster and determines whether a valid or invalid point is being targeted for the TryTeleport function</summary>
        public void ActivateRaycaster() {
            if(!raycasting) {
                line.enabled = true;
                ShowTeleport.Invoke(this);
                ToggleTeleportPoints(true);

            }

            raycasting = true;
        }

        /// <summary>Deactivates the teleport raycaster, TryTeleport cannot be used until ActivateRaycaster is called again</summary>
        public void DeactivateRaycaster() {
            if(raycasting) {
                HideTeleport.Invoke(this);
                if(reticle != null)
                    reticle.SetActive(false);
                line.enabled = false;
                ToggleTeleportPoints(false);
            }

            raycasting = false;
        }



        /// <summary>Updates the raycaster target and visuals based on given settings, checks every frame for a valid teleport </summary>
        public void UpdateRaycaster()
        {
            if (step == 0)
                return;
            float sqMaxRange = Mathf.Min(maxRange * maxRange, 500);
            float sqDistance = 0;

            List<Vector3> segments = new List<Vector3>();

            Vector3 pos = transform.position;
            Vector3 vel = transform.rotation * velocity;
            Vector3 delta;
            segments.Add(pos);

            while (sqDistance < sqMaxRange){
                delta = vel * step;
                validTeleport = Physics.Raycast(pos, delta, out RaycastHit hit, delta.magnitude, cancelTeleportLayers | teleportLayers, queryTriggers);
                if(validTeleport){
                    teleportPos = hit.point;
                    if (0 < (cancelTeleportLayers & (1 << hit.collider.gameObject.layer)))
                        validTeleport = false;
                    else
                        validTeleport = 0 < (teleportLayers &  (1 << hit.collider.gameObject.layer)) && Vector3.Angle(hit.normal, Vector3.up) < maxAngle;

                    bool foundTeleportPoint = false;
                    int hitColliderCount = Physics.OverlapSphereNonAlloc(hit.point, 0.001f, collidersNonAlloc, teleportLayers, QueryTriggerInteraction.Collide);
                    if(hitColliderCount > 0) {
                        for(int i = 0; i < hitColliderCount; i++) {
                            if(collidersNonAlloc[i].TryGetComponent(out TeleportPoint tp)) {
                                if(currentTeleportPoint == null || tp != currentTeleportPoint) {
                                    if(currentTeleportPoint != null) {
                                        currentTeleportPoint.StopHighlighting(this);
                                    }

                                    currentTeleportPoint = tp;
                                    currentTeleportPoint.StartHighlighting(this);
                                }
                                validTeleport = true;
                                foundTeleportPoint = true;
                            }
                        }
                    }

                    if(validTeleport && teleportPointOnly)
                        validTeleport = foundTeleportPoint;

                    if(!foundTeleportPoint && currentTeleportPoint != null) {
                        currentTeleportPoint.StopHighlighting(this);
                        currentTeleportPoint = null;
                    }

                    if (reticle != null){
                        reticle.transform.position = hit.point + hit.normal*0.005f;
                        reticle.transform.up = hit.normal;
                    }

                    segments.Add(hit.point);
                    break;
                }

                pos += delta;
                vel += gravity * step;
                sqDistance += delta.sqrMagnitude;
                segments.Add(pos);
            }

            if(teleportPointOnly && currentTeleportPoint == null)
                validTeleport = false;

            if(!validTeleport && currentTeleportPoint != null) {
                currentTeleportPoint.StopHighlighting(this);
                currentTeleportPoint = null;
            }

            if (line){
                line.colorGradient = validTeleport ? validColor : invalidColor;
                line.positionCount = segments.Count;
                line.SetPositions(segments.ToArray());
            }

            if (reticle)
                reticle.SetActive(validTeleport);

            if((currentTeleportPoint != null && currentTeleportPoint.matchPoint))
                reticle.SetActive(false);
        }


        /// <summary>Attempts to teleport the player to the current teleport point, returns true if the teleport was successful</summary>
        public bool TryTeleport(out Vector3 pos, out Vector3 forward)
        {
            pos = teleportPos;
            forward = Vector3.zero;
            if(currentTeleportPoint != null) {
                currentTeleportPoint.StopHighlighting(this);
                currentTeleportPoint.Teleport(this);

                if(currentTeleportPoint.teleportPoint != null) {
                    if(currentTeleportPoint.matchPoint)
                        pos = currentTeleportPoint.teleportPoint.position;

                    if(currentTeleportPoint.matchDirection)
                        forward = currentTeleportPoint.teleportPoint.forward;
                }

                currentTeleportPoint = null;
            }

            if(validTeleport)
                OnValidTeleport.Invoke(this);
            else
                OnInvalidTeleport.Invoke(this);

            return validTeleport;
        }
    }
}

