using UnityEngine;
using UnityEngine.Events;

using Foundry.Services;
using System.Collections.Generic;
using Foundry.Networking;

#if UNITY_EDITOR
using UnityEditor;
# endif

namespace Foundry
{
    public class SpatialSlider : SpatialTouchable
    {
        [System.Serializable]
        public struct SliderIncrementEvent
        {
            public NetworkEvent<int> onIncrementEnter;
            public NetworkEvent<int> onIncrementExit;

            public Transform incrementPointOnLine;
        }

        [Header("Slider Options")]
        [SerializeField] private bool grabOverride;
        public Vector3 sliderStart;
        public Vector3 sliderEnd;
        public Transform sliderVisualObject;
        [Tooltip("No increments")] public bool smoothSlider;
        public int amountOfIncrements = 10;
        
        [Space(50)]
        public SliderIncrementEvent[] sliderIncrementEvents;
        public NetworkEvent<float> sliderSmoothEvent;
        
        //Internal
        private float incrementAmount;
        private int currentIncrement;
        private SpatialInputManager spatialInputManager;
        private Vector3 sliderStartObjectSpace;
        private Vector3 sliderEndObjectSpace;

        private Vector3 sliderTargetPosition;

        bool right;

        #if UNITY_EDITOR
        private void OnValidate()
        {
            ConvertToObjectSpace();
        }
        #endif
        void ConvertToObjectSpace()
        {
            sliderEndObjectSpace = transform.TransformPoint(sliderEnd);
            sliderStartObjectSpace = transform.TransformPoint(sliderStart);

            incrementAmount = Vector3.Distance(sliderStartObjectSpace, sliderEndObjectSpace) / amountOfIncrements;
        }

        new void Start()
        {
            ConvertToObjectSpace();

            //SliderVisualObject.position = sliderStartObjectSpace;
            spatialInputManager = SpatialInputManager.instance;

            //Reset this to zero on start
            sliderSmoothEvent.Invoke(0);

            ConfigureSliderIncrements();
        }

        public override void RegisterProperties(List<INetworkProperty> props)
        {
            props.Add(sliderSmoothEvent);
            
            for (int i = 0; i < sliderIncrementEvents.Length; i++)
            {
                props.Add(sliderIncrementEvents[i].onIncrementEnter);
                props.Add(sliderIncrementEvents[i].onIncrementExit);
            }
        }

        void ConfigureSliderIncrements() 
        {
            for (int i = 1; i < sliderIncrementEvents.Length - 1; i++)
            {
                if (sliderIncrementEvents[i].incrementPointOnLine == null)
                {
                    sliderIncrementEvents[i].incrementPointOnLine = new GameObject().transform;
                    sliderIncrementEvents[i].incrementPointOnLine.parent = transform;
                    sliderIncrementEvents[i].incrementPointOnLine.name = "increment " + i;
                    sliderIncrementEvents[i].incrementPointOnLine.position = Vector3.Lerp(sliderStartObjectSpace, sliderEndObjectSpace, (1f / amountOfIncrements) * i);
                }
            }

            sliderIncrementEvents[0].incrementPointOnLine = new GameObject().transform;
            sliderIncrementEvents[0].incrementPointOnLine.position = sliderStartObjectSpace;
            sliderIncrementEvents[0].incrementPointOnLine.parent = transform;
            sliderIncrementEvents[0].incrementPointOnLine.name = "start increment";
            sliderIncrementEvents[sliderIncrementEvents.Length - 1].incrementPointOnLine = new GameObject().transform;
            sliderIncrementEvents[sliderIncrementEvents.Length - 1].incrementPointOnLine.position = sliderEndObjectSpace;
            sliderIncrementEvents[sliderIncrementEvents.Length - 1].incrementPointOnLine.parent = transform;
            sliderIncrementEvents[sliderIncrementEvents.Length - 1].incrementPointOnLine.name = "end increment";

            sliderVisualObject.position = sliderIncrementEvents[0].incrementPointOnLine.position;
        }

        public override void TouchUpdate(SpatialTouch spatialTouch)
        {
            base.TouchUpdate(spatialTouch);

            SliderUpdate(spatialTouch);
        }

        void SliderUpdate(SpatialTouch spatialTouch) 
        {
            if (grabOverride)
            {
                if (spatialTouch.SpatialHand.handType == SpatialHand.HandType.Right && spatialInputManager.grabRightXR.action.ReadValue<float>() > 0.5F || spatialTouch.SpatialHand.handType == SpatialHand.HandType.Left && spatialInputManager.grabLeftXR.action.ReadValue<float>() > 0.5F || spatialInputManager.grabDesktop.action.ReadValue<float>() > 0.5F)
                {
                    sliderTargetPosition = FindNearestPointOnLine(sliderStartObjectSpace, sliderEndObjectSpace, spatialTouch.transform.position);
                    right = sliderTargetPosition.x > 0;

                    if (smoothSlider)
                    {
                        sliderVisualObject.position = sliderTargetPosition;

                        //smooth slider event
                        sliderSmoothEvent.Invoke(Vector3.Distance(sliderVisualObject.position, sliderStartObjectSpace) / Vector3.Distance(sliderStartObjectSpace, sliderEndObjectSpace));
                    } 
                    else 
                    {
                        for (int i = 0; i < sliderIncrementEvents.Length; i++)
                        {
                            Debug.Log(Vector3.Distance(sliderTargetPosition, sliderIncrementEvents[i].incrementPointOnLine.position) < incrementAmount);
                            
                            if(Vector3.Distance(sliderTargetPosition, sliderIncrementEvents[i].incrementPointOnLine.position) < 0.1F) //if in 0.05 range of increment
                            {
                                currentIncrement = i;
                                sliderIncrementEvents[i].onIncrementEnter.Invoke(currentIncrement);
                                
                                if(currentIncrement > 0)
                                    sliderIncrementEvents[i - 1].onIncrementExit.Invoke(currentIncrement);

                                if (currentIncrement < sliderIncrementEvents.Length && !right)
                                    sliderIncrementEvents[i + 1].onIncrementExit.Invoke(currentIncrement);

                                sliderVisualObject.position = sliderIncrementEvents[i].incrementPointOnLine.position;
                            }
                        }
                    }
                }
            }
        }

        public static Vector3 FindNearestPointOnLine(Vector3 origin, Vector3 end, Vector3 point)
        {
            var heading = (end - origin);
            float magnitudeMax = heading.magnitude;
            heading.Normalize();

            var lhs = point - origin;
            float dotP = Vector3.Dot(lhs, heading);
            dotP = Mathf.Clamp(dotP, 0f, magnitudeMax);
            return origin + heading * dotP;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, 0, 0, 0.2F);
            Gizmos.DrawSphere(sliderStartObjectSpace, 0.1F);
            Gizmos.color = new Color(0, 0, 1, 0.2F);
            Gizmos.DrawSphere(sliderEndObjectSpace, 0.1F);
        }

    }


    #if UNITY_EDITOR
    [CustomEditor(typeof(SpatialSlider))]
    public class SpatialSliderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GUI.DrawTexture(GUILayoutUtility.GetRect(100, 100, 60, 60), Resources.Load<Texture2D>("FoundryHeader"), ScaleMode.ScaleToFit);

            DrawPropertiesExcluding(serializedObject, new string[] { "m_Script" });
            serializedObject.ApplyModifiedProperties();

            SpatialSlider spatialSlider = (SpatialSlider)target;

            if(spatialSlider.sliderIncrementEvents.Length != spatialSlider.amountOfIncrements + 1 && !spatialSlider.smoothSlider)
            {
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.yellow;
                GUILayout.Label("Not enough events for amount of increments please add events", style);
                
                if (GUILayout.Button("Add Events")) 
                {
                    spatialSlider.sliderIncrementEvents = new SpatialSlider.SliderIncrementEvent[spatialSlider.amountOfIncrements + 1];
                }
            }
        }
    }
    #endif
}
