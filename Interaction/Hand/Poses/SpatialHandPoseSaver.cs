using Foundry;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpatialHandPoseSaver : MonoBehaviour {
    [Header("Save Pose")]
    public SpatialHand hand;
    public Transform grabbable;
    public SpatialHandPose scriptablePose;

    [Header("Setting Pose")]
    [Range(0, 1f)]
    public float index = 1f;
    [Range(0, 1f)]
    public float middle = 1f;
    [Range(0, 1f)]
    public float ring = 1f;
    [Range(0, 1f)]
    public float pinky = 1f;
    [Range(0, 1f)]
    public float thumb = 1f;

    [ContextMenu("SAVE POSE")]
    void SavePose() {
        scriptablePose.SavePose(hand);
        Debug.Log("HAND POSE SAVED");
    }

    [ContextMenu("SAVE GRABBABLE POSE")]
    void SaveGrabbablePose() {
        scriptablePose.SavePose(hand, grabbable);
        Debug.Log("GRABBABLE POSE SAVED");
    }


    [ContextMenu("SET POSE")]
    void SetPose() {
        if(scriptablePose.poseData.Length > 0)
            scriptablePose.SetPose(hand, index, middle, ring, pinky, thumb);
    }

}
