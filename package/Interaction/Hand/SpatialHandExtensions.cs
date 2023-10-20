using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Foundry {
    public static class SpatialHandExtensions {
        public static SpatialFinger GetFinger(this SpatialHand spatialHand, SpatialFinger.FingerType finger) {
            switch(finger) {
                case SpatialFinger.FingerType.Index:
                    return spatialHand.index;
                case SpatialFinger.FingerType.Middle:
                    return spatialHand.middle;
                case SpatialFinger.FingerType.Ring:
                    return spatialHand.ring;
                case SpatialFinger.FingerType.Pinky:
                    return spatialHand.pinky;
                case SpatialFinger.FingerType.Thumb:
                    return spatialHand.thumb;
                default:
                    return null;
            }
        }
    }
}