using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry.RotationExtensions
{
    public static class RotationHelperExtensions
    {
        public static void RotateAroundLerp(Transform objectToRotate, Vector3 point, Vector3 axis, float targetAngle, float time)
        {
            float angleOffset = Mathf.LerpAngle(0, targetAngle, time * Time.deltaTime);
            Quaternion q = Quaternion.AngleAxis(angleOffset, axis);
            objectToRotate.position = q * (objectToRotate.position - point) + point;
            objectToRotate.rotation = q * objectToRotate.rotation;
        }

        public static void RotateAroundCustom(Transform objectToRotate, Vector3 point, Vector3 axis, float targetAngle)
        {
            float angleOffset = targetAngle;
            Quaternion q = Quaternion.AngleAxis(angleOffset, axis);
            objectToRotate.position = q * (objectToRotate.position - point) + point;
            objectToRotate.rotation = q * objectToRotate.rotation;
        }

        public static float WrapAngle(float angle)
        {
            angle %= 360;
            if (angle > 180)
                return angle - 360;

            return angle;
        }
    }
}
