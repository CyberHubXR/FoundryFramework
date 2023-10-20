using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Foundry.FakePhysics
{
    public class SpringJointSimulation
    {
        [System.Serializable]
        public struct LinearSpringOptions
        {
            public float springConstant;
            public float dampingFactor;
            public Vector3 targetPosition;

            public Vector3 velocity;
            public Vector3 axisOfMovement;
        }

        public static void CalculatePositionSpring(Transform springObject, LinearSpringOptions LinearSpringOptions, out float springLength) 
        {
            Vector3 displacement = LinearSpringOptions.targetPosition - springObject.position;
            springLength = displacement.magnitude;
            Vector3 springForce = LinearSpringOptions.springConstant * displacement;
            Vector3 dampingForce = -LinearSpringOptions.dampingFactor * LinearSpringOptions.velocity;

            Vector3 totalForce = springForce + dampingForce;

            // Apply the force to the object's position
            LinearSpringOptions.velocity += totalForce * Time.fixedDeltaTime;
            springObject.position += new Vector3(LinearSpringOptions.velocity.x * LinearSpringOptions.axisOfMovement.x, LinearSpringOptions.velocity.y * LinearSpringOptions.axisOfMovement.y, LinearSpringOptions.velocity.z * LinearSpringOptions.axisOfMovement.z) * Time.fixedDeltaTime;
        }
    }
}
