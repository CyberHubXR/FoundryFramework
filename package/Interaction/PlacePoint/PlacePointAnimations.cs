using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    [RequireComponent(typeof(PlacePoint))]
    public class PlacePointAnimations : InteractionAnimations {
        PlacePoint placePoint;

        protected override void OnEnable() {
            base.OnEnable();
            placePoint = GetComponent<PlacePoint>();
            placePoint.OnHighlight.AddListener(StartHighlight);
            placePoint.OnStopHighlight.AddListener(StopHighlight);
            placePoint.OnPlace.AddListener(OnPlace);
            placePoint.OnRemove.AddListener(OnRemove);
        }

        protected override void OnDisable() {
            base.OnDisable();
            placePoint.OnHighlight.RemoveListener(StartHighlight);
            placePoint.OnStopHighlight.RemoveListener(StopHighlight);
            placePoint.OnPlace.RemoveListener(OnPlace);
            placePoint.OnRemove.RemoveListener(OnRemove);
        }

        void StartHighlight(PlacePoint placePoint, SpatialGrabbable grabbable) {
            Highlight();
        }

        void StopHighlight(PlacePoint placePoint, SpatialGrabbable grabbable) {
            Unhighlight();
        }

        void OnPlace(PlacePoint placePoint, SpatialGrabbable grabbable) {
            Activate();
        }

        void OnRemove(PlacePoint placePoint, SpatialGrabbable grabbable) {
            Deactivate();
        }
    }
}
