using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using Foundry.Haptics;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace Foundry
{
    [AddComponentMenu("Foundry/Interaction/SpatialTouch")]
    public class SpatialTouch : MonoBehaviour
    {
        public List<SpatialTouchable> currentTouchables;
        
        SpatialTouchable tempTouchable;
        SpatialTouchable lastTouched;

        [HideInInspector] public SpatialHand SpatialHand;

        protected virtual void Start() 
        {
            TryGetComponent(out SpatialHand);
        
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            //Check we have a touchable on the collider
            if (other.TryGetComponent<SpatialTouchable>(out tempTouchable) && !currentTouchables.Contains(tempTouchable))
            {
                currentTouchables.Add(tempTouchable);
                StartTouch(tempTouchable);
            }
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<SpatialTouchable>(out lastTouched))
            {
                currentTouchables.Remove(lastTouched);
                tempTouchable = null;
                StopTouch(lastTouched);
                lastTouched = null;
            }
        }

        protected virtual void FixedUpdate() 
        {
            if(currentTouchables.Count > 0) 
            {
                foreach (SpatialTouchable tempTouchables in currentTouchables)
                {
                    StayTouchUpdate(tempTouchables);
                }
            }
        }


        protected virtual void StartTouch(SpatialTouchable lookable)
        {
            lookable.StartTouch(this);

            /*if(SpatialHand.handType == SpatialHand.HandType.Right)
                FoundryHaptics.SendHapticToDevice(InputSystem.GetDevice<XRController>(CommonUsages.RightHand), .5F, 0.25F);
            else
                FoundryHaptics.SendHapticToDevice(InputSystem.GetDevice<XRController>(CommonUsages.LeftHand), .5F, 0.25F);*/
        }

        protected virtual void StopTouch(SpatialTouchable lookable)
        {
            lookable.StopTouch(this);
        }

        protected virtual void StayTouchUpdate(SpatialTouchable lookable)
        {
            lookable.TouchUpdate(this);
        }
    }
}
