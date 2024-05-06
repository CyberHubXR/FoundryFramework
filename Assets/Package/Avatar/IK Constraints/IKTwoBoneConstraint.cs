using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    public class IKTwoBoneConstraint : IKTargetedConstraint
    {
        public Transform pole;
        public Transform upper;
        public Transform lower;
        public Transform end;

        public Vector3 upperBendAxis = Vector3.left;
        public Vector3 lowerBendAxis = Vector3.left;

        public struct Calibration
        {
            public float upperLength;
            public float lowerLength;
            public Quaternion upperOffset;
            public Quaternion lowerOffset;
        }
        public Calibration calibration;

        public override void Calibrate()
        {
            calibration.upperLength = (upper.position - lower.position).magnitude;
            calibration.lowerLength = (lower.position - end.position).magnitude;
            UpdateOffsetsFromAxies();
        }

        public void UpdateOffsetsFromAxies()
        {
            calibration.upperOffset = Quaternion.Inverse(Quaternion.LookRotation(Quaternion.Inverse(upper.rotation) * (lower.position - upper.position).normalized, upperBendAxis));
            calibration.lowerOffset = Quaternion.Inverse(Quaternion.LookRotation(Quaternion.Inverse(lower.rotation) * (end.position - lower.position).normalized, lowerBendAxis));
        }

        // Use cos rule to find angle in degrees
        float BendAngle(float c)
        {
            float a = calibration.lowerLength;
            float b = calibration.upperLength;
            float sqA = a * a;
            float sqB = b * b;

            float angle = Mathf.Acos((-(c * c) + sqA + sqB) / (-2 * a * b)) * Mathf.Rad2Deg;
            if (float.IsNaN(angle))
                return 0;
            return angle;
        }

        public override void Execute()
        {
            if (weight == 0)
                return;
            Vector3 targetDelta = target.position - upper.position;

            Vector3 bendNormal = Vector3.Cross(targetDelta, pole.position - upper.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(targetDelta, bendNormal);
            Quaternion upperRotation = targetRotation * calibration.upperOffset;
            Quaternion lowerRotation = targetRotation * calibration.lowerOffset;

            float bendAngle = BendAngle(targetDelta.magnitude) / 2;
            Quaternion bendRotation = Quaternion.AngleAxis(bendAngle, bendNormal);

            upperRotation = bendRotation * upperRotation;
            lowerRotation = Quaternion.Inverse(bendRotation) * lowerRotation;

            upper.rotation = Quaternion.Slerp(upper.rotation, upperRotation, weight);
            lower.rotation = Quaternion.Slerp(lower.rotation, lowerRotation, weight);

             end.rotation = Quaternion.Slerp(end.rotation, target.rotation, weight);
        }

        public float ChainLength()
        {
            return calibration.upperLength + calibration.lowerLength;
        }
    }
}

