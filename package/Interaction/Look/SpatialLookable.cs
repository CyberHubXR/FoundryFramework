using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Foundry
{
    public class SpatialLookable : MonoBehaviour
    {
        public float activationDelay = 1;
        public float deactivationDelay = 2;
        [Tooltip("Called if this object is being looked at and the activationDelay has passed.")]
        public UnityEvent<SpatialLookable> OnActivate;
        [Tooltip("Called if this object is not being looked at and the deactivationDelay has passed.")]
        public  UnityEvent<SpatialLookable> OnDeactivate;
        [Tooltip("Called when a SpatialLook starts looking at this object. This is not delayed by activationDelay and may be called before OnActivate.")]
        public UnityEvent<SpatialLook, SpatialLookable> OnStartLook;
        [Tooltip("Called when a SpatialLook stops looking at this object. This is not delayed by deactivationDelay and may be called before OnDeactivate")]
        public UnityEvent<SpatialLook, SpatialLookable> OnStopLook;
        [Tooltip("Called while one or more lookers are observing this object. This is not delayed by activationDelay and may be called before OnActivate.")]
        public UnityEvent<SpatialLook, SpatialLookable> OnLookStay;
        
        public HashSet<SpatialLook> Lookers { get; private set; } = new HashSet<SpatialLook>();
        public int LookerCount=> Lookers.Count;

        public float lookTriggerPercent { get; private set; }
        
        public bool isLooking { get { return lookTriggerPercent > 0; } }
        public bool isLookStay { get { return lookTriggerPercent >= 1; } }
        public bool isActivated { get; private set; }
        
        
        private Coroutine updateCoroutine;

        /// <summary>Triggered when the lookable has started being looked at</summary>
        public virtual void StartLook(SpatialLook spatialLook) {
            Lookers.Add(spatialLook);
            OnStartLook.Invoke(spatialLook, this);
            if (updateCoroutine == null)
                updateCoroutine = StartCoroutine(UpdateLookState());
        }

        public virtual void StopLook(SpatialLook spatialLook)
        {
            Lookers.Remove(spatialLook);
            OnStopLook.Invoke(spatialLook, this);
        }

        public virtual void LookStay(SpatialLook spatialLook) {
            OnLookStay.Invoke(spatialLook, this);
        }
        
        // This is a coroutine because when nobody is looking at this object it should have no logic running, and a coroutine is a simple way to do that.
        public IEnumerator UpdateLookState() {
            // If this object is being looked at or we are not fully deactivated yet.

            while(Lookers.Count > 0 || lookTriggerPercent > 0) {
                if(Lookers.Count > 0) {
                    lookTriggerPercent = Mathf.Clamp01(lookTriggerPercent + Time.deltaTime / activationDelay);
                }
                else {
                    lookTriggerPercent = Mathf.Clamp01(lookTriggerPercent - Time.deltaTime / deactivationDelay);
                }
                
                if(lookTriggerPercent >= 1) {
                    if(!isActivated) {
                        isActivated = true;
                        OnActivate.Invoke(this);
                    }
                }

                if (lookTriggerPercent > 0 && Lookers.Count > 0) {
                    foreach(var looker in Lookers)
                        LookStay(looker);
                }
                    
                
                yield return null;
            }

            isActivated = false;
            OnDeactivate.Invoke(this);
            updateCoroutine = null;
        }
    }
}
