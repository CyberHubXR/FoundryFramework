using Foundry;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR;

[System.Serializable]
public struct FingerPoseData {
    public Vector3[] targetPositions;
    public Quaternion[] targetRotations;
}


[CreateAssetMenu(fileName = "NEW HAND POSE", menuName = "Foundry/Hand Pose", order = 1)]
public class SpatialHandPose : ScriptableObject {
    [FormerlySerializedAs("rightPoseData")]
    public FingerPoseData[] poseData;
    //public FingerPoseData[] leftPoseData;

    public Vector3 relativeOffset;
    public Quaternion relativeRotation = Quaternion.identity;


    public void SavePose(SpatialHand hand, Transform grabbable) {
        relativeOffset = hand.transform.InverseTransformPoint(grabbable.transform.position);
        relativeRotation = Quaternion.Inverse(hand.transform.rotation) * grabbable.transform.rotation;

        SavePose(hand);
    }

    public void SavePose(SpatialHand hand) {
        poseData = new FingerPoseData[5];

        SaveFinger(hand, hand.index);
        SaveFinger(hand, hand.middle);
        SaveFinger(hand, hand.ring);
        SaveFinger(hand, hand.pinky);
        SaveFinger(hand, hand.thumb);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    public void SaveFinger(SpatialHand hand, SpatialFinger finger) {
        finger.InitializeTransforms();

        int points = 0;
        int fingerIndex = (int)finger.fingerType;
        GetKidsCount(finger.transform, finger.fingerTip.transform, ref points);

        poseData[fingerIndex].targetPositions = new Vector3[points];
        poseData[fingerIndex].targetRotations = new Quaternion[points];
            
        int i = 0;
        if(hand.handType == SpatialHand.HandType.Right) {
            AssignChildrenPose(finger.transform, ref i, ref poseData[(int)finger.fingerType].targetPositions, ref poseData[(int)finger.fingerType].targetRotations, Vector3.one);
        }
        else {
            AssignChildrenPose(finger.transform, ref i, ref poseData[(int)finger.fingerType].targetPositions, ref poseData[(int)finger.fingerType].targetRotations, new Vector3(1, -1, -1));
        }


        void AssignChildrenPose(Transform obj, ref int index, ref Vector3[] targetPosition, ref Quaternion[] targetRotation, Vector3 inverter) {
            if(!obj.Equals(finger.fingerTip.transform)) {
                targetPosition[index] = new Vector3(obj.localPosition.x * inverter.x, obj.localPosition.y * inverter.y, obj.localPosition.z * inverter.z);
                targetRotation[index] = new Quaternion(obj.localRotation.x * inverter.x, obj.localRotation.y * inverter.y, obj.localRotation.z * inverter.z, obj.localRotation.w);
                index++;
                for(int j = 0; j < obj.childCount; j++) 
                    AssignChildrenPose(obj.GetChild(j), ref index, ref targetPosition, ref targetRotation, inverter);
            }
        }
    }


    public Vector3[] GetTargetPositions(SpatialHand.HandType handType, SpatialFinger.FingerType finger) {
        return poseData[(int)finger].targetPositions;
    }

    public Quaternion[] GetTargetRotations(SpatialHand.HandType handType, SpatialFinger.FingerType finger) {
        return poseData[(int)finger].targetRotations;
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



    public void SetPose(SpatialHand hand, SpatialGrabbable grabbable) {
        if(hand.handType == SpatialHand.HandType.Left) {
            var postition = relativeOffset;
            postition.x *= -1;
            grabbable.transform.position = hand.transform.TransformPoint(postition);
            var rotation = relativeRotation;
            rotation.z *= -1;
            rotation.y *= -1;
            grabbable.transform.rotation = hand.transform.rotation * rotation;
        }
        else {
            grabbable.transform.position = hand.transform.TransformPoint(relativeOffset);
            grabbable.transform.rotation = hand.transform.rotation * relativeRotation;
        }

        grabbable.attachedRigidbody.position = grabbable.transform.position;
        grabbable.attachedRigidbody.rotation = grabbable.transform.rotation;

        SetPose(hand);
    }

    public void SetPose(SpatialHand hand, float index = 1f, float middle = 1f, float ring = 1f, float pinky = 1f, float thumb = 1f) {
        //var targetData = hand.handType == SpatialHand.HandType.Right ? rightPoseData : leftPoseData;
        var targetData = poseData ;

        if(index > 0) 
            for(int i = 0; i < targetData[(int)SpatialFinger.FingerType.Index].targetRotations.Length; i++) {
                var targetRotation = targetData[(int)SpatialFinger.FingerType.Index].targetRotations[i];
                if(hand.handType == SpatialHand.HandType.Left) {
                    targetRotation.y *= -1;
                    targetRotation.z *= -1;
                }
                hand.index.fingerJoints[i].localRotation = Quaternion.Lerp(hand.index.fingerJoints[i].localRotation, targetRotation, index);
            }
        if(middle > 0)
            for(int i = 0; i < targetData[(int)SpatialFinger.FingerType.Middle].targetRotations.Length; i++) {
                var targetRotation = targetData[(int)SpatialFinger.FingerType.Middle].targetRotations[i];
                if(hand.handType == SpatialHand.HandType.Left) {
                    targetRotation.y *= -1;
                    targetRotation.z *= -1;
                }
                hand.middle.fingerJoints[i].localRotation = Quaternion.Lerp(hand.middle.fingerJoints[i].localRotation, targetRotation, middle);
            }
        if(ring > 0)
            for(int i = 0; i < targetData[(int)SpatialFinger.FingerType.Ring].targetRotations.Length; i++) {
                var targetRotation = targetData[(int)SpatialFinger.FingerType.Ring].targetRotations[i];
                if(hand.handType == SpatialHand.HandType.Left) {
                    targetRotation.y *= -1;
                    targetRotation.z *= -1;
                }
                hand.ring.fingerJoints[i].localRotation = Quaternion.Lerp(hand.ring.fingerJoints[i].localRotation, targetRotation, ring);
            }
        if(pinky > 0)
            for(int i = 0; i < targetData[(int)SpatialFinger.FingerType.Pinky].targetRotations.Length; i++) {
                var targetRotation = targetData[(int)SpatialFinger.FingerType.Pinky].targetRotations[i];
                if(hand.handType == SpatialHand.HandType.Left) {
                    targetRotation.y *= -1;
                    targetRotation.z *= -1;
                }
                hand.pinky.fingerJoints[i].localRotation = Quaternion.Lerp(hand.pinky.fingerJoints[i].localRotation, targetRotation, pinky);
            }
        if(thumb > 0)
            for(int i = 0; i < targetData[(int)SpatialFinger.FingerType.Thumb].targetRotations.Length; i++) {
                var targetRotation = targetData[(int)SpatialFinger.FingerType.Thumb].targetRotations[i];
                if(hand.handType == SpatialHand.HandType.Left) {
                    targetRotation.y *= -1;
                    targetRotation.z *= -1;
                }
                hand.thumb.fingerJoints[i].localRotation = Quaternion.Lerp(hand.thumb.fingerJoints[i].localRotation, targetRotation, thumb);
            }
    }



    public void LerpPose(SpatialHand hand, SpatialGrabbable grabbable, SpatialHandPose to, float point) {
        grabbable.transform.rotation = hand.transform.rotation * Quaternion.Lerp(relativeRotation, to.relativeRotation, point);
        grabbable.transform.position =  hand.transform.TransformPoint(Vector3.Lerp(relativeOffset, to.relativeOffset, point));

        LerpPose(hand, this, to, point);
    }

    public void LerpPose(SpatialHand hand, SpatialHandPose to, float point) {
        LerpPose(hand, this, to, point);
    }



    public void BlendPose(SpatialHandPose to, float point, ref SpatialHandPose newPose, float index = 1f, float middle = 1f, float ring = 1f, float pinky = 1f, float thumb = 1f) {
        if(newPose.relativeRotation != Quaternion.identity) {
            newPose.relativeOffset = Vector3.Lerp(relativeOffset, to.relativeOffset, point);
            newPose.relativeRotation = Quaternion.Lerp(relativeRotation, to.relativeRotation, point);
        }

        if(index > 0) {
            int i = (int)SpatialFinger.FingerType.Index;
            for(int j = 0; j < poseData[i].targetRotations.Length; j++) {
                newPose.poseData[i].targetRotations[j] = Quaternion.Lerp(poseData[i].targetRotations[j], to.poseData[i].targetRotations[j], point*index);
            }
        }
        if(middle > 0) {
            int i = (int)SpatialFinger.FingerType.Middle;
            for(int j = 0; j < poseData[i].targetRotations.Length; j++) {
                newPose.poseData[i].targetRotations[j] = Quaternion.Lerp(poseData[i].targetRotations[j], to.poseData[i].targetRotations[j], point*middle);
            }
        }
        if(ring > 0) {
            int i = (int)SpatialFinger.FingerType.Ring;
            for(int j = 0; j < poseData[i].targetRotations.Length; j++) {
                newPose.poseData[i].targetRotations[j] = Quaternion.Lerp(poseData[i].targetRotations[j], to.poseData[i].targetRotations[j], point*ring);
            }
        }
        if(pinky > 0) {
            int i = (int)SpatialFinger.FingerType.Pinky;
            for(int j = 0; j < poseData[i].targetRotations.Length; j++) {
                newPose.poseData[i].targetRotations[j] = Quaternion.Lerp(poseData[i].targetRotations[j], to.poseData[i].targetRotations[j], point*pinky);
            }
        }
        if(thumb > 0) {
            int i = (int)SpatialFinger.FingerType.Thumb;
            for(int j = 0; j < poseData[i].targetRotations.Length; j++) {
                newPose.poseData[i].targetRotations[j] = Quaternion.Lerp(poseData[i].targetRotations[j], to.poseData[i].targetRotations[j], point*thumb);
            }
        }
    }




    //STATICS
    public static void CreateInstance(SpatialHand hand, out SpatialHandPose newPose) {
        newPose = CreateInstance<SpatialHandPose>();
        newPose.SavePose(hand);
    }


    public static void LerpPose(SpatialHand hand, SpatialGrabbable grabbable, SpatialHandPose from, SpatialHandPose to, float point) {
        grabbable.transform.position = hand.transform.TransformPoint(to.relativeOffset);
        grabbable.transform.rotation = hand.transform.rotation * Quaternion.Lerp(from.relativeRotation, to.relativeRotation, point);
        grabbable.transform.position =  hand.transform.TransformPoint(Vector3.Lerp(from.relativeOffset, to.relativeOffset, point));

        LerpPose(hand, from, to, point);
    }


    public static void LerpPose(SpatialHand hand, SpatialHandPose from, SpatialHandPose to, float point, float index = 1f, float middle = 1f, float ring = 1f, float pinky = 1f, float thumb = 1f) {
        var toData = to.poseData;
        var fromData = from.poseData;

        if(index > 0) {
            for(int i = 0; i < toData[(int)SpatialFinger.FingerType.Index].targetPositions.Length; i++) {
                hand.index.fingerJoints[i].localRotation = Quaternion.Lerp(fromData[(int)SpatialFinger.FingerType.Index].targetRotations[i], toData[(int)SpatialFinger.FingerType.Index].targetRotations[i], point*index);
            }
        }
        if(middle > 0) {
            for(int i = 0; i < toData[(int)SpatialFinger.FingerType.Middle].targetPositions.Length; i++) {
                hand.middle.fingerJoints[i].localRotation = Quaternion.Lerp(fromData[(int)SpatialFinger.FingerType.Middle].targetRotations[i], toData[(int)SpatialFinger.FingerType.Middle].targetRotations[i], point*middle);
            }
        }
        if(ring > 0) {
            for(int i = 0; i < toData[(int)SpatialFinger.FingerType.Ring].targetPositions.Length; i++) {
                hand.ring.fingerJoints[i].localRotation = Quaternion.Lerp(fromData[(int)SpatialFinger.FingerType.Ring].targetRotations[i], toData[(int)SpatialFinger.FingerType.Ring].targetRotations[i], point*ring);
            }
        }
        if(pinky > 0) {
            for(int i = 0; i < toData[(int)SpatialFinger.FingerType.Pinky].targetPositions.Length; i++) {
                hand.pinky.fingerJoints[i].localRotation = Quaternion.Lerp(fromData[(int)SpatialFinger.FingerType.Pinky].targetRotations[i], toData[(int)SpatialFinger.FingerType.Pinky].targetRotations[i], point*pinky);
            }
        }
        if(thumb > 0) {
            for(int i = 0; i < toData[(int)SpatialFinger.FingerType.Thumb].targetPositions.Length; i++) {
                hand.thumb.fingerJoints[i].localRotation = Quaternion.Lerp(fromData[(int)SpatialFinger.FingerType.Thumb].targetRotations[i], toData[(int)SpatialFinger.FingerType.Thumb].targetRotations[i], point*thumb);
            }
        }
    }

    public void CopyPose(SpatialHandPose pose) {
        relativeOffset = pose.relativeOffset;
        relativeRotation = pose.relativeRotation;

        for(int i = 0; i < poseData.Length; i++) {
            for(int j = 0; j < poseData[i].targetRotations.Length; j++) {
                poseData[i].targetRotations[j] = pose.poseData[i].targetRotations[j];
                poseData[i].targetPositions[j] = pose.poseData[i].targetPositions[j];
            }
        }
    }

    public void MoveTowards(SpatialHandPose currentPose, float moveTowards, ref SpatialHandPose results) {

        for(int i = 0; i < poseData.Length; i++) {
            for(int j = 0; j < poseData[i].targetRotations.Length; j++) {
                poseData[i].targetRotations[j] = Quaternion.RotateTowards(poseData[i].targetRotations[j], currentPose.poseData[i].targetRotations[j], moveTowards);
            }
        }
    }
    public void MoveTowardsAngleDistanceCurve(SpatialHandPose currentPose, float minDelta, float maxDelta, float distanceMulti, ref SpatialHandPose results) {

        for(int i = 0; i < poseData.Length; i++) {
            for(int j = 0; j < poseData[i].targetRotations.Length; j++) {
                var angle = Quaternion.Angle(poseData[i].targetRotations[j], currentPose.poseData[i].targetRotations[j]);
                var point = Mathf.Clamp(minDelta + angle * distanceMulti, minDelta, maxDelta);
                poseData[i].targetRotations[j] = Quaternion.RotateTowards(poseData[i].targetRotations[j], currentPose.poseData[i].targetRotations[j], point);
            }
        }
    }
}
