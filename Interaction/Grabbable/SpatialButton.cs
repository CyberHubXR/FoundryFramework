using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

using Foundry.Networking;
using System.Collections.Generic;

namespace Foundry
{
    public class SpatialButton : SpatialTouchable
    {
        [Space]
        [Header("Button Options")]
        [Space]
        public Vector3 buttonBottomPoint;
        public Vector3 buttonTopPoint;
        [Space]
        public Transform buttonVisualObject;
        [Space]
        [Range(0,1)] public float buttonActivationPoint = .5F;
        [Space]
        public NetworkEvent<bool> StartButtonPressed;
        public NetworkEvent<bool> StopButtonPressed;

        public float buttonPressValue;
        
        private float buttonTravelLength;
        private Vector3 buttonTopPointObjectSpace;
        private Vector3 buttonBottomPointObjectSpace;
        private Vector3 buttonTravel;
        private bool buttonPressed;

        SpatialTouch hand;

        public NetworkObject Object { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ConvertToObjectSpace();
        }
#endif

        new void Start()
        {
            ConvertToObjectSpace();
        }

        public override void TouchUpdate(SpatialTouch spatialTouch)
        {
            base.TouchUpdate(spatialTouch);
            ButtonPressLogic(spatialTouch);
            hand = spatialTouch;
        }


        void ConvertToObjectSpace()
        {
            buttonTopPointObjectSpace = transform.TransformPoint(buttonTopPoint);
            buttonBottomPointObjectSpace = transform.TransformPoint(buttonBottomPoint);

            buttonTravelLength = Vector3.Distance(buttonTopPointObjectSpace, buttonBottomPointObjectSpace);
        }

        void ButtonPressLogic(SpatialTouch hand)
        {
            buttonTravel = FindNearestPointOnLine(buttonBottomPointObjectSpace, buttonTopPointObjectSpace, hand.transform.position);
            buttonVisualObject.position = buttonTravel;
        }

        private void Update()
        {
            //only check distance to ensure this doesnt run all the time
            if (!isTouching && Vector3.Distance(buttonVisualObject.position, buttonTopPoint) > 0.05F)
            {
                buttonVisualObject.position = Vector3.Lerp(buttonVisualObject.position, buttonTopPointObjectSpace, 15 * Time.deltaTime);       
            }


            CheckButtonPress();
        }

        void CheckButtonPress() 
        {
            buttonPressValue = Vector3.Distance(buttonVisualObject.position, buttonTopPointObjectSpace) / buttonTravelLength;

            if (buttonPressValue > buttonActivationPoint && !buttonPressed)
            {
                StartButtonPressed.Invoke(this);
                buttonPressed = true;
            }

            if (buttonPressValue < buttonActivationPoint && buttonPressed)
            {
                StopButtonPressed.Invoke(this);
                buttonPressed = false;
            }
        }

        public void TestDepressButton() 
        {
            buttonVisualObject.position = buttonBottomPointObjectSpace;
        }

        public void ResetButton()
        {
            buttonVisualObject.position = buttonTopPointObjectSpace;
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
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(buttonBottomPointObjectSpace, 0.035F);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(buttonTopPointObjectSpace, 0.035F);
        }

        public override void RegisterProperties(List<INetworkProperty> props, List<INetworkEvent> events)
        {
            base.RegisterProperties(props, events);
            events.Add(StartButtonPressed);
            events.Add(StopButtonPressed);
        }
    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(SpatialButton))]
    public class SpatialButtonEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GUI.DrawTexture(GUILayoutUtility.GetRect(100, 100, 60, 60), Resources.Load<Texture2D>("FoundryHeader"), ScaleMode.ScaleToFit);

            DrawPropertiesExcluding(serializedObject, new string[] { "m_Script" });
            serializedObject.ApplyModifiedProperties();

            GUILayout.Space(10);
            GUILayout.Label("Debugging Tools", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Depress Button"))
            {
                SpatialButton spatialButton = (SpatialButton)target;
                spatialButton.TestDepressButton();
            }
            if (GUILayout.Button("Reset"))
            {
                SpatialButton spatialButton = (SpatialButton)target;
                spatialButton.ResetButton();
            }
        }

    }
    #endif
}
