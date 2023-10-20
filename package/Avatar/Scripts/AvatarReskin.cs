using System;
using System.Collections.Generic;
using Foundry;
using UnityEngine;

public class AvatarReskin : MonoBehaviour
{
    private Transform[] boneTargets;
    private Transform[] renderBones;

    private readonly HumanBodyBones[] updateOrder = {
        HumanBodyBones.Hips,
        HumanBodyBones.LeftUpperLeg,
        HumanBodyBones.LeftLowerLeg,
        HumanBodyBones.LeftFoot,
        HumanBodyBones.LeftToes,
        HumanBodyBones.RightUpperLeg,
        HumanBodyBones.RightLowerLeg,
        HumanBodyBones.RightFoot,
        HumanBodyBones.RightToes,
        HumanBodyBones.Spine,
        HumanBodyBones.Chest,
        HumanBodyBones.UpperChest,
        HumanBodyBones.Neck,
        HumanBodyBones.Head,
        HumanBodyBones.LeftEye,
        HumanBodyBones.RightEye,
        HumanBodyBones.Jaw,
        HumanBodyBones.LeftShoulder,
        HumanBodyBones.LeftUpperArm,
        HumanBodyBones.LeftLowerArm,
        HumanBodyBones.LeftHand,
        HumanBodyBones.RightShoulder,
        HumanBodyBones.RightUpperArm,
        HumanBodyBones.RightLowerArm,
        HumanBodyBones.RightHand,
        HumanBodyBones.LeftThumbProximal,
        HumanBodyBones.LeftThumbIntermediate,
        HumanBodyBones.LeftThumbDistal,
        HumanBodyBones.LeftIndexProximal,
        HumanBodyBones.LeftIndexIntermediate,
        HumanBodyBones.LeftIndexDistal,
        HumanBodyBones.LeftMiddleProximal,
        HumanBodyBones.LeftMiddleIntermediate,
        HumanBodyBones.LeftMiddleDistal,
        HumanBodyBones.LeftRingProximal,
        HumanBodyBones.LeftRingIntermediate,
        HumanBodyBones.LeftRingDistal,
        HumanBodyBones.LeftLittleProximal,
        HumanBodyBones.LeftLittleIntermediate,
        HumanBodyBones.LeftLittleDistal,
        HumanBodyBones.RightThumbProximal,
        HumanBodyBones.RightThumbIntermediate,
        HumanBodyBones.RightThumbDistal,
        HumanBodyBones.RightIndexProximal,
        HumanBodyBones.RightIndexIntermediate,
        HumanBodyBones.RightIndexDistal,
        HumanBodyBones.RightMiddleProximal,
        HumanBodyBones.RightMiddleIntermediate,
        HumanBodyBones.RightMiddleDistal,
        HumanBodyBones.RightRingProximal,
        HumanBodyBones.RightRingIntermediate,
        HumanBodyBones.RightRingDistal,
        HumanBodyBones.RightLittleProximal,
        HumanBodyBones.RightLittleIntermediate,
        HumanBodyBones.RightLittleDistal
    };
    private void Start()
    {
        var avatar = GetComponentInParent<FullBodyAvatar>();
        var avatarAnim = avatar.animator;
        var animator = GetComponent<Animator>();

        // Commenting this out to prevent rpm reference for the moment, it will still function, serious need for refactors here
        /* if (GetComponent<AvatarData>()) 
            avatar.SetAnimatorAvatar(animator.avatar); */ 

        List<Transform> fromList = new();
        List<Transform> toList = new();
        foreach (var bone in updateOrder)
        {
            var from = avatarAnim.GetBoneTransform(bone);
            var to = animator.GetBoneTransform(bone);
            if (!from || !to)
                continue;
            fromList.Add(from);
            toList.Add(to);
        }
        
        boneTargets = fromList.ToArray();
        renderBones = toList.ToArray();
        ResizeRig();
        
        
        avatar.CalibrateRig(animator);
        Destroy(animator);
    }

    void ResizeRig()
    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        for (int i = 0; i < boneTargets.Length; i++)
            MatchBoneTRS(boneTargets[i], renderBones[i]);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    private void LateUpdate()
    {
        MatchAvatarBonesToRootFBT();
    }

    void MatchBoneTR(Transform target, Transform source)
    {
        Debug.DrawLine(target.position, source.position, Color.red);
        target.position = source.position;
        target.rotation = source.rotation;
    }

    void MatchBoneTRS(Transform target, Transform source)
    {
        MatchBoneTR(target, source);
        target.localScale = source.localScale;
    }

    [BeforeRenderOrder(Int32.MaxValue)]
    void MatchAvatarBonesToRootFBT()
    {
        for (int i = 0; i < boneTargets.Length; i++)
            MatchBoneTRS(renderBones[i], boneTargets[i]);
    }
}
