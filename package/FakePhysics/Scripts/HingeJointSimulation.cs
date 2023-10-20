using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Foundry.FakePhysics
{
    public class HingeJointSimulation : MonoBehaviour
    {
        [Header("Joint Settings")]
        public Vector3 anchor;
        public bool connectedToWorld;

        [Header("Body Settings")]
        public float bodyMass = 1F;
        public float bodyDrag = 0.1F;
        public float bodyAngularDrag = 0.1F;

        [Header("Solver Settings")]
        public int angularVelocitySolverIteractions = Physics.defaultSolverIterations;

        Vector3 spaceAnchor;
        public Vector3 gravityConstant = new Vector3(0, -9.81F, 0);

        [Header("DEBUG IGNORE")]
        public Vector3[] angularVelocityBuffer;
        public int bufferIndex;
        public Vector3 currentAngularVelocity;
        public Vector3 initialAngularVelocity = Vector3.zero;

        private void OnValidate()
        {
            spaceAnchor = transform.position + anchor;
        }

        private void Start()
        {
            currentAngularVelocity = initialAngularVelocity;
            angularVelocityBuffer = new Vector3[angularVelocitySolverIteractions];
        }

        private void FixedUpdate()
        {
            CalculateAngularVelocity();

            CalculateHingeJoint();
        }

        void CalculateAngularVelocity() 
        {
            // Store the current angular velocity in the buffer
            angularVelocityBuffer[bufferIndex] = currentAngularVelocity;
            bufferIndex = (bufferIndex + 1) % angularVelocitySolverIteractions;

            // Calculate the average angular velocity from the buffer
            Vector3 averageAngularVelocity = Vector3.zero;
            for (int i = 0; i < angularVelocitySolverIteractions; i++)
            {
                averageAngularVelocity += angularVelocityBuffer[i];
            }
            averageAngularVelocity /= angularVelocitySolverIteractions;

            // Update the current angular velocity with the average angular velocity
            currentAngularVelocity = averageAngularVelocity;
        }

        void CalculateHingeJoint() 
        {
            Vector3 pivotPosition = spaceAnchor;
            Vector3 hingePosition = transform.position;

            // Calculate the direction vector from the hinge to the pivot
            Vector3 pivotToHinge = hingePosition - pivotPosition;

            // Calculate the current rotation angles based on the current angular velocity
            Vector3 rotationAmount = currentAngularVelocity * Time.deltaTime;

            // Apply gravity to the current angular velocity
            currentAngularVelocity += gravityConstant * Time.deltaTime;

            // Rotate the hinged object around the pivot using the current angular velocity
            transform.RotateAround(pivotPosition, transform.up, rotationAmount.y);

            /* Ensure that the hinged object maintains its distance from the pivot
            Vector3 newHingePosition = pivotPosition + Quaternion.Euler(0, 0, rotationAmount.z) * pivotToHinge;
            transform.position = newHingePosition;*/
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, 0, 0, 0.2F);
            Gizmos.DrawSphere(spaceAnchor, 0.1F);
        }
    }
}
