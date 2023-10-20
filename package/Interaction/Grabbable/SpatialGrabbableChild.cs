using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Foundry {
    /// <summary>
    /// THIS SCRIPT CAN BE ATTACHED TO A COLLIDER OBJECT TO REFERENCE A GRABBABLE BODY
    /// </summary>
    [DefaultExecutionOrder(1)]
    public class SpatialGrabbableChild : MonoBehaviour {
        public SpatialGrabbable grabParent;

        private void Start() {
            grabParent.SetGrabbableChild(this);
            if(gameObject.layer == LayerMask.NameToLayer("Default") || LayerMask.LayerToName(gameObject.layer) == "")
                gameObject.layer = LayerMask.NameToLayer(SpatialHand.SpatialGrabbableLayerNameDefault);
        }
    }
}
