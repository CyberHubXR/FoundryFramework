using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry {
    
    [System.Serializable]
    public struct VelocityTimePair {
        public float time;
        public Vector3 velocity;
    }
    
    public class HandVelocityTracker {
        SpatialHand hand = null;
        float minThrowVelocity = 0f;

        ///<summary> A list of all acceleration values from the time the throwing motion was detected til now.</summary>
        protected List<VelocityTimePair> m_ThrowVelocityList = new List<VelocityTimePair>();
        protected List<VelocityTimePair> m_ThrowAngleVelocityList = new List<VelocityTimePair>();

        public void ClearThrow() {
            m_ThrowVelocityList.Clear();
            m_ThrowAngleVelocityList.Clear();
        }

        float disableTime;
        float disableSeconds;
        public void Disable(float seconds) {
            disableTime = Time.realtimeSinceStartup;
            disableSeconds = seconds;
            ClearThrow();
        }

        public HandVelocityTracker(SpatialHand hand) {
            this.hand = hand;
        }


        public void UpdateThrowing() {
            if(disableTime + disableSeconds > Time.realtimeSinceStartup) {
                if(m_ThrowVelocityList.Count > 0) {
                    m_ThrowVelocityList.Clear();
                    m_ThrowAngleVelocityList.Clear();
                }
                return;
            }

            if(hand.held == null) {
                if(m_ThrowVelocityList.Count > 0) {
                    m_ThrowVelocityList.Clear();
                    m_ThrowAngleVelocityList.Clear();
                }

                return;
            }

            // Add current hand velocity to throw velocity list.
            m_ThrowVelocityList.Add(new VelocityTimePair() { time = Time.realtimeSinceStartup, velocity = hand.held == null ? Vector3.zero : hand.transform.position });

            // Remove old entries from m_ThrowVelocityList.
            for(int i = m_ThrowVelocityList.Count - 1; i >= 0; --i) {
                if(Time.realtimeSinceStartup - m_ThrowVelocityList[i].time >= hand.throwVelocityExpireTime) {
                    // Remove expired entry.
                    m_ThrowVelocityList.RemoveAt(i);
                }
            }

            // Add current hand velocity to throw velocity list.
            m_ThrowAngleVelocityList.Add(new VelocityTimePair() { time = Time.realtimeSinceStartup, velocity = hand.held == null ? Vector3.zero : hand.transform.localEulerAngles });

            // Remove old entries from m_ThrowVelocityList.
            for(int i = m_ThrowAngleVelocityList.Count - 1; i >= 0; --i) {
                if(Time.realtimeSinceStartup - m_ThrowAngleVelocityList[i].time >= hand.throwAngularVelocityExpireTime) {
                    // Remove expired entry.
                    m_ThrowAngleVelocityList.RemoveAt(i);
                }
            }
        }

        /// <summary>Returns the hands velocity times its strength</summary>
        public Vector3 ThrowVelocity() {
            if(hand.held == null)
                return Vector3.zero;

            // Calculate the average hand velocity over the course of the throw.
            Vector3 averageVelocity = Vector3.zero;
            Vector3 totalVelocity = Vector3.zero;
            if(m_ThrowVelocityList.Count > 0) {
                for (int i = 1; i < m_ThrowVelocityList.Count; i++)
                {
                    Vector3 velocity = m_ThrowVelocityList[i].velocity - m_ThrowVelocityList[i - 1].velocity;
                    totalVelocity += velocity;
                }
                averageVelocity =  totalVelocity / (m_ThrowVelocityList.Count - 1);
            }

            var vel = averageVelocity * hand.throwStrength;

            return vel.magnitude > minThrowVelocity ? vel : Vector3.zero;
        }

        /// <summary>Returns the hands velocity times its strength</summary>
        public Vector3 ThrowAngularVelocity() {
            if(hand.held == null)
                return Vector3.zero;

            // Calculate the average hand velocity over the course of the throw.
            Vector3 averageVelocity = Vector3.zero;
            Vector3 totalAngularVelocity = Vector3.zero;
            
            for(int i = 1; i < m_ThrowAngleVelocityList.Count; i++)
            {
                Vector3 angularVelocity = m_ThrowAngleVelocityList[i].velocity - m_ThrowAngleVelocityList[i - 1].velocity;

               /* averageVelocity = new Vector3(WrapAngle(angularVelocity.x), WrapAngle(angularVelocity.y),
                    WrapAngle(angularVelocity.z));*/
                
                totalAngularVelocity += angularVelocity;
            }

            averageVelocity = totalAngularVelocity / (m_ThrowVelocityList.Count - 1) * hand.throwAngularStrength;

            return averageVelocity.magnitude > minThrowVelocity ? averageVelocity : Vector3.zero;
        }

        float WrapAngle(float angle)
        {
            angle %= 360;
            
            if (angle > 180)
                return angle - 360;

            return angle;
        }
    }
}