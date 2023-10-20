using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Foundry
{
    [RequireComponent(typeof(SpatialGrabbable))]
    public class SpatialGrabbableOutlineEffect : MeshOutline
    {
        public Color startHighlightColor = Color.white;
        public Color endHighlightColor = Color.white;
        public AnimationCurve highlightColorCurve = AnimationCurve.Linear(0, 0, 1, 1);

        public float highlightWidth = 0.01f;
        public AnimationCurve highlightWidthCurve = AnimationCurve.Linear(0, 0, 1, 1);

        public float highlightTime = 0.5f;
        public float unhighlightTime = 0.5f;

        SpatialGrabbable grabbable;
        bool highlighting = false;
        float highlightStartTime = 0;
        float highlightStopTime = 0;
        float highlightState;

        protected virtual void Start() {
            if(grabbable == null)
                grabbable = GetComponent<SpatialGrabbable>();
            if(grabbable == null)
                grabbable = GetComponentInParent<SpatialGrabbable>();

            grabbable.OnFirstHighlightEvent.AddListener(OnHighlight);
            grabbable.OnFinalStopHighlightEvent.AddListener(OnUnhighlight);
        }

        protected override void OnEnable() {
            base.OnEnable(); 

            highlightState = 0;
            OutlineWidth = highlightWidth * highlightWidthCurve.Evaluate(highlightState);
            OutlineColor = Color.Lerp(startHighlightColor, endHighlightColor, highlightColorCurve.Evaluate(highlightState));

        }


        protected override void Update() {
            base.Update();
            bool updatedState = false;
            //UPDATE POSITON STATES
            if(highlighting && highlightStartTime + highlightTime > Time.time-Time.deltaTime) {
                highlightState += Time.deltaTime / highlightTime;
                highlightState = Mathf.Clamp01(highlightState);
                updatedState = true;
            }
            else if(!highlighting && highlightStopTime + unhighlightTime > Time.time-Time.deltaTime) {
                highlightState -= Time.deltaTime / unhighlightTime;
                highlightState = Mathf.Clamp01(highlightState);
                updatedState = true;
            }

            if(updatedState) {
                OutlineWidth = highlightWidth * highlightWidthCurve.Evaluate(highlightState);
                OutlineColor = Color.Lerp(startHighlightColor, endHighlightColor, highlightColorCurve.Evaluate(highlightState));
            }

            if(!highlighting && highlightState == 0)
                enabled = false;
        }

        [ContextMenu("DEBUG_HIGHLIGHT")]
        public void DEBUG_HIGHLIGHT() {

            highlighting = true;
            highlightStartTime = Time.time;
            enabled = true;
        }

        [ContextMenu("DEBUG_UNHIGHLIGHT")]
        public void DEBUG_UNHIGHLIGHT() {
            highlighting = false;
            highlightStopTime = Time.time;
        }

        void OnHighlight(SpatialHand hand, SpatialGrabbable grab) {
            highlighting = true;
            highlightStartTime = Time.time;
            enabled = true;
        }

        void OnUnhighlight(SpatialHand hand, SpatialGrabbable grab) {
            highlighting = false;
            highlightStopTime = Time.time;
        }
    }
}
