using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    public class SpatialGrabbablePose : MonoBehaviour
    {
        public SpatialHandPose pose;

        public void SetPose(SpatialHandPose pose) {
            this.pose = pose;
        }
    }
}
