using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    public class SpatialTouchableAnimation : InteractionAnimations
    {
        [Header("Reference")]
        public SpatialTouchable spatialTouchable;

        protected override void OnEnable() {
            base.OnEnable();
            if(spatialTouchable == null)
                spatialTouchable = GetComponent<SpatialTouchable>();
           spatialTouchable.OnStartTouch.AddListener(OnTouchStart);
           spatialTouchable.OnStopTouch.AddListener(OnTouchStop);
           spatialTouchable.OnTouchStay.AddListener(OnStay);
        }

        protected override void OnDisable() {
            base.OnDisable();
            spatialTouchable.OnStartTouch.RemoveListener(OnTouchStart);
            spatialTouchable.OnStopTouch.RemoveListener(OnTouchStop);
            spatialTouchable.OnTouchStay.RemoveListener(OnStay);
        }

        public void OnTouchStart(NetEventSource src, bool b) {
            Highlight();
        }

        public void OnTouchStop(NetEventSource src, bool b) {
            Unhighlight();
            Deactivate();
        }

        public void OnStay(NetEventSource src, bool b) {
            Activate();
        }
    }
}
