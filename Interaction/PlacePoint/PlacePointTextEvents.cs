using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    public class PlacePointTextEvents : MonoBehaviour
    {
        public PlacePoint placePoint;
        public TMPro.TextMeshPro highlightText;
        public TMPro.TextMeshPro placementText;

        public string placeText = "PLACED";
        public string removedText = "EMPTY";
        public string highlightingText = "HIGHLIGHTING";
        public string stopHighlightText = "";

        void Awake()
        {
            if (placePoint == null) {
                TryGetComponent(out placePoint);
            }
        }

        void OnEnable() {
            placePoint.OnHighlight.AddListener(OnHighlight);
            placePoint.OnStopHighlight.AddListener(OnStopHighlight);
            placePoint.OnPlace.AddListener(OnPlaced);
            placePoint.OnRemove.AddListener(OnRemoved);
        }

        void OnDisable() {
            placePoint.OnHighlight.RemoveListener(OnHighlight);
            placePoint.OnStopHighlight.RemoveListener(OnStopHighlight);
            placePoint.OnPlace.RemoveListener(OnPlaced);
            placePoint.OnRemove.RemoveListener(OnRemoved);
        }


        void OnPlaced(PlacePoint placePoint, SpatialGrabbable grabbable) {
            placementText.text = placeText;
        }

        void OnRemoved(PlacePoint placePoint, SpatialGrabbable grabbable) {
            placementText.text = removedText;
        }

        void OnHighlight(PlacePoint placePoint, SpatialGrabbable grabbable) {
            highlightText.text = highlightingText;
        }

        void OnStopHighlight(PlacePoint placePoint, SpatialGrabbable grabbable) {
            highlightText.text = stopHighlightText;
        }
    }
}
