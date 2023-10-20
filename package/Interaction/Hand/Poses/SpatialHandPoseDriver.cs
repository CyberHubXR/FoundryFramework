using ExitGames.Client.Photon.StructWrapping;
using Foundry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Foundry.Networking;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public struct HandPoseInput {
    public InputActionProperty input;
    public SpatialHandPose pose;
    [Space]
    [Range(0, 1)]
    public float indexIgnore;
    [Range(0, 1)]
    public float middleIgnore;
    [Range(0, 1)]
    public float ringIgnore;
    [Range(0, 1)]
    public float pinkyIgnore;
    [Range(0, 1)]
    public float thumbIgnore;

}

public class SpatialHandPoseDriver : NetworkComponent {
    [Header("Hand")]
    public SpatialHand hand;
    public SpatialHandPose openHandPose;
    public SpatialHandPose closedHandPose;

    [Header("Grab")]
    public bool enableGrabPose = true;

    [Header("Highlight")]
    [Tooltip("Whether or not to enable the highlight pose indicator")]
    public bool enableHighlightPose = true;
    [Tooltip("Highlight pose is a transition of the open hand and the target grab pose, this offset represents the percent to blend the pose when highlighting")]
    public float highlightPoseOffset = 0.2f;
    [Tooltip("How long to maintain the transition pose")]
    public float highlightPoseTime = 0.18f;

    [Header("Input")]
    [Tooltip("Hand Pose Inputs will blend all given poses based on the (0-1) value os their inputs")]
    public HandPoseInput[] inputPoses;

    [Header("Noise")]
    [Tooltip("Whether or not to apply noise to the hand while using input poses")]
    public bool enablePoseNoise = true;
    [Space]
    [Tooltip("Determines the Amplitude of the blend offset between 0-1. Uses perlin noise to offset the blending state between open and closed hand before applying input offsets. Blend1 and blend2 results are added together")]
    public float blendAmp1 = 0.15f;
    [Tooltip("Determines the Frequency of the blend offset noise. Uses perlin noise to offset the blending state between open and closed hand before applying input offsets. Blend1 and blend2 results are added together")]
    public float blendFreq1 = 14f;
    [Space]
    [Tooltip("Determines the Amplitude of the blend offset between 0-1. Uses perlin noise to offset the blending state between open and closed hand before applying input offsets. Blend1 and blend2 results are added together")]
    public float blendAmp2 = 0.1f;
    [Tooltip("Determines the Frequency of the blend offset noise. Uses perlin noise to offset the blending state between open and closed hand before applying input offsets. Blend1 and blend2 results are added together")]
    public float blendFreq2 = 7f;
    [Space]
    [Tooltip("Determines the Amplitude of the finger offset between 0-1. Uses perlin noise to offset the finger ignore state between open and closed hand before applying input offsets. Blend1 and blend2 results are added together")]
    public float fingerOffsetAmp = 0.2f;
    [Tooltip("Determines the Frequency of the finger offset noise. Uses perlin noise to offset the finger ignore state between open and closed hand before applying input offsets. Blend1 and blend2 results are added together")]
    public float fingerOffsetFreq = 10f;

    NetworkArray<float> inputValues;

    SpatialHandPose currentTargetPose = null;
    SpatialHandPose tempPose = null;

    bool wasGrabPoseEnabled = true;
    bool wasHighlightPoseEnabled = true;
    int lastInputCount = 0;

    public override void RegisterProperties(List<INetworkProperty> props)
    {
        if(inputValues == null)
            inputValues = new NetworkArray<float>(inputPoses.Length);
        props.Add(inputValues);
    }

    void OnEnable() {
        //Create pose instances
        SpatialHandPose.CreateInstance(hand, out currentTargetPose);
        SpatialHandPose.CreateInstance(hand, out tempPose);
        
        if(inputValues == null)
            inputValues = new NetworkArray<float>(inputPoses.Length);

        //Enable inputs
        for(int i = 0; i < inputPoses.Length; i++) {
            var inputPose = inputPoses[i];
            inputPose.input.action.Enable();
        }

        //Add listeners
        if(enableGrabPose) {
            hand.OnBeforeGrabbedEvent.AddListener(OnGrab);
            hand.OnReleaseEvent.AddListener(OnRelease);
        }
        if(enableHighlightPose) {
            hand.OnHighlightEvent.AddListener(OnHighlight);
            hand.OnStopHighlightEvent.AddListener(OnUnhighlight);
        }

        wasGrabPoseEnabled = enableGrabPose;
        wasHighlightPoseEnabled = enableHighlightPose;
        lastInputCount = inputPoses.Length;
    }

    void OnDisable() {
        if(enableGrabPose) {
            hand.OnBeforeGrabbedEvent.RemoveListener(OnGrab);
            hand.OnReleaseEvent.RemoveListener(OnRelease);
        }
        if(enableHighlightPose) {
            hand.OnHighlightEvent.RemoveListener(OnHighlight);
            hand.OnStopHighlightEvent.RemoveListener(OnUnhighlight);
        }
    }



    void OnDestroy() {
        Destroy(currentTargetPose);
        Destroy(tempPose);
    }



    void Update() {
        if(hand.held == null && (!enableHighlightPose || !hand.IsHighlighting()) && !hand.poseAnimator.IsAnimating()) {
            MatchInputPose();
        }

        CheckValueChange();
    }


    public void MatchInputPose() {
        if(openHandPose == null || closedHandPose == null)
            return;

        float inputCount = 0;
        for(int i = 0; i < inputPoses.Length; i++) {
            var inputPose = inputPoses[i];

            if (IsOwner)
                inputValues[i] = inputPose.input.action.ReadValue<float>();
            float value = inputValues[i];
            
            if(value > 0) {
                inputCount += (1-inputPose.thumbIgnore)/10f;
                inputCount += (1-inputPose.indexIgnore)/10f;
                inputCount += (1-inputPose.middleIgnore)/10f;
                inputCount += (1-inputPose.ringIgnore)/10f;
                inputCount += (1-inputPose.pinkyIgnore)/10f;
            }
        }
        inputCount = Mathf.Clamp(inputCount, 1, 3);

        if(enablePoseNoise) {
            var noise1 = Mathf.PerlinNoise(Time.time/blendFreq1, 34.5f)*blendAmp1/inputCount;
            var noise2 = Mathf.PerlinNoise(Time.time/blendFreq2, 0.5f)*blendAmp2/inputCount;

            var indexNoise = Mathf.Abs(Mathf.PerlinNoise(Time.time/fingerOffsetFreq, 1.1f)*fingerOffsetAmp)/inputCount;
            var middleNoise = Mathf.Abs(Mathf.PerlinNoise(Time.time/fingerOffsetFreq, 5.2f)*fingerOffsetAmp)/inputCount;
            var ringNoise = Mathf.Abs(Mathf.PerlinNoise(Time.time/fingerOffsetFreq, 4.3f)*fingerOffsetAmp)/inputCount;
            var pinkyNoise = Mathf.Abs(Mathf.PerlinNoise(Time.time/fingerOffsetFreq, 7.4f)*fingerOffsetAmp)/inputCount;
            var thumbNoise = Mathf.Abs(Mathf.PerlinNoise(Time.time/fingerOffsetFreq, 3.6f)*fingerOffsetAmp)/inputCount;

            openHandPose.BlendPose(closedHandPose,
            noise1 + noise2,
            ref currentTargetPose,
            1-indexNoise,
            1-middleNoise,
            1-ringNoise,
            1-pinkyNoise,
            1-thumbNoise);
        }
        else {
            openHandPose.BlendPose(closedHandPose, 0, ref currentTargetPose);
        }

        for(int i = 0; i < inputPoses.Length; i++) {
            var inputPose = inputPoses[i];
            var value = inputValues[i];
            if(value > 0)
                currentTargetPose.BlendPose(inputPose.pose, value/inputCount, ref currentTargetPose, 1-inputPose.indexIgnore, 1-inputPose.middleIgnore, 1-inputPose.ringIgnore, 1-inputPose.pinkyIgnore, 1-inputPose.thumbIgnore);
        }

        hand.poseAnimator.UpdateTargetPose(currentTargetPose);
    }

    void CheckValueChange() {
        if(wasGrabPoseEnabled != enableGrabPose) {
            wasGrabPoseEnabled = enableGrabPose;
            if(enableGrabPose) {
                hand.OnBeforeGrabbedEvent.AddListener(OnGrab);
                hand.OnReleaseEvent.AddListener(OnRelease);
            }
            else {
                hand.OnBeforeGrabbedEvent.RemoveListener(OnGrab);
                hand.OnReleaseEvent.RemoveListener(OnRelease);
            }
        }

        if(wasHighlightPoseEnabled != enableHighlightPose) {
            wasHighlightPoseEnabled = enableHighlightPose;
            if(enableHighlightPose) {
                hand.OnHighlightEvent.AddListener(OnHighlight);
                hand.OnStopHighlightEvent.AddListener(OnUnhighlight);
            }
            else {
                hand.OnHighlightEvent.RemoveListener(OnHighlight);
                hand.OnStopHighlightEvent.RemoveListener(OnUnhighlight);
            }
        }

        if(lastInputCount != inputPoses.Length) {
            lastInputCount = inputPoses.Length;
            for(int i = 0; i < inputPoses.Length; i++) {
                var inputPose = inputPoses[i];
                inputPose.input.action.Enable();
            }
        }

        wasGrabPoseEnabled = enableGrabPose;
        wasHighlightPoseEnabled = enableHighlightPose;
        lastInputCount = inputPoses.Length;
    }



    void OnHighlight(SpatialHand hand, SpatialGrabbable grabbable) {

        if(hand.highlighterEnabled) {
            openHandPose.BlendPose(closedHandPose, 0.05f, ref tempPose);
            hand.poseAnimator.SetPose(tempPose, highlightPoseTime*1f);

            if(grabbable.TryGetComponent(out SpatialGrabbablePose pose)) {
                openHandPose.BlendPose(pose.pose, highlightPoseOffset, ref tempPose);
                hand.poseAnimator.AddToQueue(tempPose, highlightPoseTime*2f);
            }
            else {
                openHandPose.BlendPose(closedHandPose, highlightPoseOffset, ref tempPose);
                hand.poseAnimator.AddToQueue(tempPose, highlightPoseTime*2f);
            }
        }
    }

    void OnUnhighlight(SpatialHand hand, SpatialGrabbable grabbable) { }



    void OnGrab(SpatialHand hand, SpatialGrabbable grabbable) {
        if(grabbable.TryGetComponent(out SpatialGrabbablePose pose)) {
            openHandPose.BlendPose(pose.pose, UnityEngine.Random.value*0.1f, ref tempPose);
            pose.pose.SetPose(hand, grabbable);
            
            hand.poseAnimator.SetPose(tempPose, 0.05f);

            openHandPose.BlendPose(pose.pose, 1f, ref tempPose);
            hand.poseAnimator.AddToQueue(tempPose, 0, true);
        }
        else {
            openHandPose.BlendPose(closedHandPose, UnityEngine.Random.value*0.1f, ref tempPose);

            hand.poseAnimator.SetPose(tempPose, 0.05f);
            
            openHandPose.BlendPose(closedHandPose, 1f, ref tempPose);
            hand.poseAnimator.AddToQueue(tempPose, 0, true);
        }
    }

    void OnRelease(SpatialHand hand, SpatialGrabbable grabbable) {
        openHandPose.BlendPose(closedHandPose, 0.9f, ref tempPose);
        hand.poseAnimator.SetPose(tempPose, 0);
    }

}
