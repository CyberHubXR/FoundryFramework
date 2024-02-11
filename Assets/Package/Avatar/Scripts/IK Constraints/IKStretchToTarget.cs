using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    public class IKStretchToTarget : IKConstraint
    {
        public IKTwoBoneConstraint constraint;

        [Tooltip("Max scaling of the stretch bone")]
        public float maxStretch = 1.5f;
        public bool copyTargetWeight = true;

        struct Calibration
        {
            public float chainLength;
            public float boneLength;
            public Vector3 boneDelta;
        }
        private Calibration calibration;

        [Tooltip("Sets how much more we should stretch relitive to the ratio of target distance to resting bone length, useful to avoid locking out joints")]
        public AnimationCurve overstrech;

        public override void Calibrate()
        {
            calibration.boneDelta = Quaternion.Inverse(constraint.upper.rotation) * (constraint.lower.position - constraint.upper.position);
            calibration.boneLength = calibration.boneDelta.magnitude;
            calibration.chainLength = (constraint.lower.position - constraint.end.position).magnitude + calibration.boneLength;
        }

        public override void Execute()
        {
            if (copyTargetWeight)
                weight = constraint.weight;
            if (weight == 0)
                return;

            float distance = (constraint.upper.position - constraint.target.position).magnitude;

            float delta = distance - calibration.chainLength;
            float scale = (delta + calibration.boneLength) / calibration.boneLength;

            //Clamp before overstrech to allow users to define an "understretch"
            scale = Mathf.Clamp(scale, 1, maxStretch);
            scale *= overstrech.Evaluate(scale);
            scale = Mathf.Lerp(1, scale, weight);

            constraint.lower.position = constraint.upper.position + constraint.upper.rotation * calibration.boneDelta * scale;
            constraint.calibration.upperLength = calibration.boneLength * scale;
        }
    }
}

