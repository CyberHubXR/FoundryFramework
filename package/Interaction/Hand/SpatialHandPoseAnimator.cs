using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{

    [RequireComponent(typeof(SpatialHand))]
    public class SpatialHandPoseAnimator : MonoBehaviour
    {
        public AnimationCurve defaultCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [HideInInspector]
        public SpatialHandPose currentPose;
        public bool maintainPose { get; private set; }


        SpatialHand hand;
        SpatialHandPose toPose;
        SpatialHandPose fromPose;
        SpatialHandPose smoothPose;

        float animationState = 0;
        float animationTime = 0;
        float startAnimationTime = 0;

        Queue<SpatialHandPose> poseQueue = new Queue<SpatialHandPose>();
        Queue<float> timeQueue = new Queue<float>();
        Queue<bool> maintainQueue = new Queue<bool>();


        void Awake() {
            hand = GetComponent<SpatialHand>();
            SpatialHandPose.CreateInstance(hand, out fromPose);
            SpatialHandPose.CreateInstance(hand, out toPose);
            SpatialHandPose.CreateInstance(hand, out currentPose);
            SpatialHandPose.CreateInstance(hand, out smoothPose);
        }

        void OnDestroy() {
            Destroy(fromPose);
            Destroy(toPose);
            Destroy(currentPose);
            Destroy(smoothPose);
        }

        void LateUpdate() {
            UpdatePoseState();
        }


        void UpdatePoseState() {
            if((Time.time - startAnimationTime) / animationTime < 1) {
                animationState = (Time.time - startAnimationTime) / animationTime;
                animationState = Mathf.Clamp01(animationState);
                var animationPoint = defaultCurve.Evaluate(animationState);
            }
            else if(poseQueue.Count > 0) {
                var nextPose = poseQueue.Dequeue();
                var nextTime = timeQueue.Dequeue();
                var nextMaintain = maintainQueue.Dequeue();
                SetPose(nextPose, nextTime, false, nextMaintain);
                Destroy(nextPose);
            }
            else {
                animationState = 1;
                var animationPoint = defaultCurve.Evaluate(animationState);
                fromPose.LerpPose(hand, toPose, animationPoint);
            }

            smoothPose.MoveTowardsAngleDistanceCurve(toPose, Time.deltaTime * 5f, Time.deltaTime*1200f, 0.18f, ref smoothPose);
            smoothPose.SetPose(hand);
            currentPose.SavePose(hand);

        }




        /// <summary>Sets the pose of the hand to the given pose</summary>
        /// <param name="pose">The pose to set</param>
        /// <param name="poseTime">The amount of time this pose will be held</param>
        /// <param name="clearQueue">Whether or not this pose should clear any poses currently queued to be played after this pose</param>
        /// <param name="maintainPose">Whether or not this pose should be maintained until the ClearPose() function is called</param>
        public void SetPose(SpatialHandPose pose, float poseTime, bool clearQueue = true, bool maintainPose = false) {
            fromPose.BlendPose(toPose, animationState, ref fromPose);
            toPose.CopyPose(pose);
            startAnimationTime = Time.time;
            animationTime = poseTime;
            animationState = 0;
            this.maintainPose = maintainPose;
            fromPose.SetPose(hand);

            if(clearQueue)
                for(int i = 0; i < poseQueue.Count; i++) {
                    var queuedPose = poseQueue.Dequeue();
                    var queuedTime = timeQueue.Dequeue();
                    var queuedMaintain = maintainQueue.Dequeue();
                    Destroy(queuedPose);
                }
        }

        public void ClearPose() {
            maintainPose = false;
        }




        /// <summary>Adds a pose to the queue to be played after the current pose and all other poses in the queue have been played</summary>
        /// <param name="pose">The pose to set</param>
        /// <param name="poseTime">The amount of time this pose will be held</param>
        /// <param name="maintainPose">Whether or not this pose should be maintained until the ClearPose() function is called</param>
        public void AddToQueue(SpatialHandPose pose, float time, bool maintainPose = false) {
            SpatialHandPose.CreateInstance(hand, out var queuedPose);
            queuedPose.CopyPose(pose);
            poseQueue.Enqueue(queuedPose);
            timeQueue.Enqueue(time);
            maintainQueue.Enqueue(maintainPose);
        }



        /// <summary>Updates the current pose without resetting the animation, this is important for transitioning to a pose that interpolates between two poses and can change while the animation is occuring</summary>
        public void UpdateTargetPose(SpatialHandPose pose) {
            toPose.CopyPose(pose);
        }



        /// <summary>Returns true if the hand is currently or scheduled to maintain a pose</summary>
        public bool IsAnimating() {
            return (animationState < 1 || poseQueue.Count > 0 || maintainPose);
        }

    }
}
