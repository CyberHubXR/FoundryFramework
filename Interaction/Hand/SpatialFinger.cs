using Foundry;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;




public class SpatialFinger : MonoBehaviour
{
    public enum FingerType {
        Index,
        Middle,
        Ring,
        Pinky,
        Thumb
    }


    public SphereCollider fingerTip;
    public FingerType fingerType;
    public Transform[] fingerJoints;

    public SpatialHand hand { get; internal set; }

    public void Awake() {
        InitializeTransforms();
    }


    public float GetScaledFingerTipRadius() {
        return fingerTip.radius * Mathf.Max(Mathf.Abs(fingerTip.transform.lossyScale.x), Mathf.Abs(fingerTip.transform.lossyScale.y), Mathf.Abs(fingerTip.transform.lossyScale.z));
    }

    public void InitializeTransforms() {
        int points = 0;
        GetKidsCount(transform, fingerTip.transform, ref points);
        //Debug.Log(transform.name + " POINTS: " + points);
        fingerJoints = new Transform[points];

        int i = 0;
        AssignChildrenPose(transform, ref i, ref fingerJoints);
        void AssignChildrenPose(Transform obj, ref int index, ref Transform[] fingerJoints) {
            if(!obj.Equals(fingerTip.transform)) {
                //Debug.Log(obj.name +  " :INDEX: " + index);
                fingerJoints[index] = obj;
                index++;
                for(int j = 0; j < obj.childCount; j++)
                    AssignChildrenPose(obj.GetChild(j), ref index, ref fingerJoints);
            }
        }
    }


    int GetKidsCount(Transform obj, Transform stopObj, ref int count) {
        if(!obj.Equals(stopObj.transform)) {
            count++;
            for(int k = 0; k < obj.childCount; k++) {
                GetKidsCount(obj.GetChild(k), stopObj, ref count);
            }
        }
        return count;
    }


    public void LerpPose(SpatialHandPose fromPose, SpatialHandPose toPose, float point) {
        var fromPoseRot = fromPose.GetTargetRotations(hand.handType, fingerType);
        var toPoseRot = toPose.GetTargetRotations(hand.handType, fingerType);
        for(int j = 0; j < fingerJoints.Length; j++)
            fingerJoints[j].localRotation = Quaternion.Lerp(fromPoseRot[j], toPoseRot[j], point);

    }
}
