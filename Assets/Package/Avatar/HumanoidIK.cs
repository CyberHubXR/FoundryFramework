using System.Collections;
using System.Collections.Generic;
using System.IO;
using Foundry.Core.Serialization;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;

namespace Foundry
{
    [System.Serializable]
    public struct PosRot: IFoundrySerializable
    {
        public Vector3 pos;
        public Quaternion rot;

        public IFoundrySerializer GetSerializer()
        {
            return new Serializer();
        }

        private struct Serializer : IFoundrySerializer
        {
            public void Serialize(in object value, BinaryWriter writer)
            {
                var posRot = (PosRot)value;
                var v3s = new Vector3Serializer();
                var qs = new QuaternionSerializer();
                v3s.Serialize(posRot.pos, writer);
                qs.Serialize(posRot.rot, writer);
            }

            public void Deserialize(ref object value, BinaryReader reader)
            {
                var posRot = (PosRot)value;
                var v3s = new Vector3Serializer();
                var qs = new QuaternionSerializer();
                object v = posRot.pos;
                v3s.Deserialize(ref v, reader);
                posRot.pos = (Vector3)v;
                object r = posRot.rot;
                qs.Deserialize(ref r, reader);
                posRot.rot = (Quaternion)r;
            }
        }
    }

    [DefaultExecutionOrder(-500)]
    public class HumanoidIK : MonoBehaviour
    {
        [System.Serializable]
        public struct References
        {
            public Transform head;
            public Transform leftHand;
            public Transform rightHand;
            public Transform waist;
            public Transform leftFoot;
            public Transform rightFoot;
        }

        [System.Serializable]
        public struct Constraints
        {
            public IKTwoBoneConstraint head;
            public IKTwoBoneConstraint leftHand;
            public IKTwoBoneConstraint rightHand;
            public IKTransformConstraint waist;
            public IKTwoBoneConstraint leftFoot;
            public IKTwoBoneConstraint rightFoot;
        }

        [System.Serializable]
        public struct Calibration
        {
            [HideInInspector]
            public bool calibrated;
            
            public bool autoSetFootRests;
            public PosRot leftFootRest;
            public PosRot rightFootRest;

            [FormerlySerializedAs("autoSetSpine")] public bool autoSetSpinePole;
            public Vector3 spinePoleOffset;
            public Vector3 lowerSpineAxis;

            public bool autoSetHeadHeight;
            public float headHeight;
            public bool autoSetWaistDelta;
            public PosRot waistDelta;
        }

        enum MoveState
        {
            FeetPlanted,
            MovingLeft,
            MovingRight
        };

        [System.Serializable]
        struct State
        {
            public float rotation;

            // Foot tracking state
            public MoveState moveState;
            public float footLerpWeight;
            public PosRot footStartPos;

            public PosRot leftFoot;
            public PosRot rightFoot;

            public Vector3 centerOfMassOffset;

            public Vector3 hipVelocity;
            public Vector3 lastHipPosition;

            public Vector3 virtualVelocity;
            public float walkAnimSpeed;

            public bool isWalking;
        }

        public References ikRefs;
        public Constraints constraints;
        public Calibration calibration;
        [Tooltip("If full body tracking is enabled, feet and waist will be placed using trackers")]
        public bool fbt = false;
        public float maxFootDistance = 0.2f;
        public float maxFootAngle = 30;
        public float lerpTime = 0.3f;
        public float velocityPredictionSmoothing = 0.1f;
        public float rotationBuffer = 30f;

        public AnimationCurve footLift;
        public AnimationCurve footAngle;
        public AnimationCurve waistAngle;

        public Animator animator;
        private State state;
        private IKExecutor executor;

        public Vector3 virtualVelocity {
            get => state.virtualVelocity;
            set
            {
                state.virtualVelocity = value;
            }
        }

        private PosRot GetRelative(Transform reference, Transform t)
        {
            Quaternion rotOffset = Quaternion.Inverse(reference.rotation);
            return new PosRot
            {
                pos = rotOffset * (t.position - reference.position),
                rot = rotOffset * t.rotation
            };
        }

        private PosRot GetGlobal(Transform t)
        {
            return new PosRot
            {
                pos = t.position,
                rot = t.rotation
            };
        }

        private void SetRelativeTransform(Transform t, in PosRot delta)
        {
            t.position = transform.position + transform.rotation * delta.pos;
            t.rotation = transform.rotation * delta.rot;
        }

        private void SetGlobalTransform(Transform t, in PosRot delta)
        {
            t.position = delta.pos;
            t.rotation = delta.rot;
        }

        public void Calibrate(Animator avatarAnimator, bool setRestPoses = true)
        {
            calibration.calibrated = true;
            
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;

            var avatarRoot = avatarAnimator.transform;
            var avatarHead = avatarAnimator.GetBoneTransform(HumanBodyBones.Head);
            var avatarWaist = avatarAnimator.GetBoneTransform(HumanBodyBones.Hips);

            if (setRestPoses)
            {
                if (calibration.autoSetFootRests)
                {
                    var avatarLeftFoot = avatarAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
                    var avatarRightFoot = avatarAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
                    calibration.leftFootRest = GetRelative(avatarRoot, avatarLeftFoot);
                    calibration.rightFootRest = GetRelative(avatarRoot, avatarRightFoot);
                    state.leftFoot = GetGlobal(avatarLeftFoot);
                    state.rightFoot = GetGlobal(avatarRightFoot);
                    
                }

                if (calibration.autoSetSpinePole)
                {
                    constraints.waist.target.position = avatarWaist.position;
                    constraints.waist.target.rotation = avatarWaist.rotation;
                    constraints.head.target.position = avatarHead.position;
                    constraints.head.target.rotation = avatarHead.rotation;
                    calibration.spinePoleOffset = Quaternion.Inverse(avatarRoot.rotation) * (constraints.head.pole.position - constraints.waist.target.position);
                    calibration.lowerSpineAxis = constraints.head.upperBendAxis;
                }
            }

            constraints.head.upperBendAxis = calibration.lowerSpineAxis;
            constraints.head.UpdateOffsetsFromAxies();
            if (calibration.autoSetWaistDelta)
            {
                calibration.waistDelta = new PosRot
                {
                    pos = Quaternion.Inverse(avatarHead.rotation) * Vector3.down * (avatarWaist.position - avatarHead.position).magnitude,
                    rot = Quaternion.Inverse(avatarHead.rotation) * avatarWaist.rotation
                };
                Debug.DrawRay(avatarHead.position, calibration.waistDelta.pos);
            }
            

            if(calibration.autoSetHeadHeight)
                calibration.headHeight = (avatarHead.position - avatarRoot.position).magnitude;

            if (!fbt)
            {
                SetRelativeTransform(ikRefs.leftFoot, calibration.leftFootRest);
                SetRelativeTransform(ikRefs.rightFoot, calibration.rightFootRest);
            }

            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == "Walking")
                {
                    state.walkAnimSpeed = clip.apparentSpeed;
                    break;
                }
            }

            state.lastHipPosition = ikRefs.waist.position;
            GetComponent<IKExecutor>().Calibrate();
        }

        private void Awake()
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            executor = GetComponent<IKExecutor>();
            executor.mode = IKExecutor.ExecutionTime.Manual;
        }

        private void Start()
        {
            var player = GetComponentInParent<Player>();
            if(player)
            {
                player.onAfterTeleport.AddListener((Player player) =>
                {
                    SetRelativeTransform(ikRefs.leftFoot, calibration.leftFootRest);
                    SetRelativeTransform(ikRefs.rightFoot, calibration.rightFootRest);

                    state.lastHipPosition = ikRefs.waist.position;
                    state.hipVelocity = Vector3.zero;
                    state.leftFoot = GetGlobal(ikRefs.leftFoot);
                    state.rightFoot = GetGlobal(ikRefs.rightFoot);
                    state.moveState = MoveState.FeetPlanted;
                    state.footLerpWeight = 0;
                    state.footStartPos = GetGlobal(ikRefs.leftFoot);
                });
            }
        }

        bool ShouldMoveFoot(Transform foot, bool isLeft, in PosRot restDelta)
        {
            Vector3 footDeltaPos = Quaternion.Inverse(transform.rotation) * (foot.position - transform.position - state.centerOfMassOffset);

            // Check if a foot is crossed over
            if (isLeft && footDeltaPos.x > 0)
                return true;
            if (!isLeft && footDeltaPos.x < 0)
                return true;

            // Check if we're too far away from our resting pos
            if ((footDeltaPos - restDelta.pos).sqrMagnitude > maxFootDistance * maxFootDistance)
                return true;
            Quaternion deltaAngle = Quaternion.Inverse(transform.rotation) * foot.rotation;
            float angle = deltaAngle.eulerAngles.y;
            if (angle > 180)
                angle -= 360;

            // Check if we're at an uncomfortable rotation, accountin for the fact that inner rotation is more uncomfortable
            if (isLeft && (angle < -maxFootAngle || angle > 15))
                return true;
            if (!isLeft && (angle > maxFootAngle || angle < -15))
                return true;
            return false;
        }

        void MoveFoot(ref PosRot foot, in PosRot restDelta)
        {
            // Infinite loop can be cause by single frame jumps
            state.footLerpWeight += Mathf.Min((1 / lerpTime) * Time.deltaTime, 0.999f);
            float smoothedWeight = Mathf.Sin(state.footLerpWeight * (Mathf.PI / 2));
            Vector3 footTarget = transform.position + (transform.rotation * restDelta.pos) + (state.hipVelocity * (lerpTime / 2)) + state.centerOfMassOffset;
            foot.pos = Vector3.Lerp(state.footStartPos.pos, footTarget, smoothedWeight) + Vector3.up * footLift.Evaluate(smoothedWeight);
            foot.rot = Quaternion.Slerp(state.footStartPos.rot, transform.rotation * restDelta.rot, smoothedWeight) * Quaternion.AngleAxis(footAngle.Evaluate(smoothedWeight), Vector3.left);
            if (state.footLerpWeight >= 1.0f)
            {
                state.moveState = MoveState.FeetPlanted;
                foot.pos = footTarget;
                foot.rot = transform.rotation * restDelta.rot;
            }
        }

        void PoseFeet()
        {
            if (state.isWalking)
                return;

            switch(state.moveState)
            {
                case MoveState.FeetPlanted:
                    if (ShouldMoveFoot(ikRefs.leftFoot, true, calibration.leftFootRest))
                    {
                        state.moveState = MoveState.MovingLeft;
                        state.footLerpWeight = 0;
                        state.footStartPos = new PosRot
                        {
                            pos = ikRefs.leftFoot.position,
                            rot = ikRefs.leftFoot.rotation
                        };
                        goto case MoveState.MovingLeft;
                    }
                    if (ShouldMoveFoot(ikRefs.rightFoot, false, calibration.rightFootRest))
                    {
                        state.moveState = MoveState.MovingRight;
                        state.footLerpWeight = 0;
                        state.footStartPos = new PosRot
                        {
                            pos = ikRefs.rightFoot.position,
                            rot = ikRefs.rightFoot.rotation
                        };
                        goto case MoveState.MovingRight;
                    }
                    break;
                case MoveState.MovingLeft:
                    MoveFoot(ref state.leftFoot, calibration.leftFootRest);
                    if(state.moveState == MoveState.FeetPlanted && ShouldMoveFoot(ikRefs.rightFoot, false, calibration.rightFootRest))
                    {
                        state.moveState = MoveState.MovingRight;
                        state.footLerpWeight = 0;
                        state.footStartPos = new PosRot
                        {
                            pos = ikRefs.rightFoot.position,
                            rot = ikRefs.rightFoot.rotation
                        };
                        goto case MoveState.MovingRight;
                    }
                    break;
                case MoveState.MovingRight:
                    MoveFoot(ref state.rightFoot, calibration.rightFootRest);
                    if (state.moveState == MoveState.FeetPlanted && ShouldMoveFoot(ikRefs.leftFoot, true, calibration.leftFootRest))
                    {
                        state.moveState = MoveState.MovingLeft;
                        state.footLerpWeight = 0;
                        state.footStartPos = new PosRot
                        {
                            pos = ikRefs.leftFoot.position,
                            rot = ikRefs.leftFoot.rotation
                        };
                        goto case MoveState.MovingLeft;
                    }
                    break;
            }
            ikRefs.leftFoot.rotation = state.leftFoot.rot;
            ikRefs.leftFoot.position = state.leftFoot.pos;
            ikRefs.rightFoot.rotation = state.rightFoot.rot;
            ikRefs.rightFoot.position = state.rightFoot.pos;
        }

        void PoseWaist()
        {
            float pushWeight = Mathf.Clamp01(1 - transform.InverseTransformPoint(ikRefs.head.position).y / calibration.headHeight);
            Quaternion pushRotation = Quaternion.AngleAxis(waistAngle.Evaluate(pushWeight), Vector3.right);

            
            ikRefs.waist.position = ikRefs.head.position + transform.rotation * (pushRotation * calibration.waistDelta.pos);

            state.hipVelocity = velocityPredictionSmoothing * state.hipVelocity + ((1f - velocityPredictionSmoothing) * ((ikRefs.waist.position - state.lastHipPosition) / Mathf.Max(0.001f, Time.smoothDeltaTime)));
            state.lastHipPosition = ikRefs.waist.position;

            ikRefs.waist.rotation = transform.rotation * pushRotation * calibration.waistDelta.rot;

            state.centerOfMassOffset = Vector3.ProjectOnPlane(ikRefs.waist.position - transform.position, transform.up);
        }

        void RotateChest()
        {
            if (constraints.leftHand.weight == 0 || constraints.rightHand.weight == 0)
                return;

            Vector3 leftDelta = Quaternion.Inverse(transform.rotation) * Vector3.ProjectOnPlane(ikRefs.leftHand.position - constraints.head.lower.position, transform.up).normalized;
            float leftAngle = Mathf.Acos(Vector3.Dot(leftDelta, Vector3.forward)) * (Vector3.Dot(leftDelta, Vector3.right) > 0 ? 1 : -1) * Mathf.Rad2Deg;
            if (float.IsNaN(leftAngle))
                leftAngle = 0;

            Vector3 rightDelta = Quaternion.Inverse(transform.rotation) * Vector3.ProjectOnPlane(ikRefs.rightHand.position - constraints.head.lower.position, transform.up).normalized;
            float rightAngle = Mathf.Acos(Vector3.Dot(rightDelta, Vector3.forward)) * (Vector3.Dot(rightDelta, Vector3.right) > 0 ? 1 : -1) * Mathf.Rad2Deg;
            if (float.IsNaN(rightAngle))
                rightAngle = 0;

            //Debug.Log("left: " + leftAngle + ", right: " + rightAngle);

            if (leftAngle > 90)
                leftAngle -= 360;
            if (rightAngle < -90)
                rightAngle += 360;

            //Debug.DrawRay(constraints.head.lower.position, transform.rotation * Quaternion.AngleAxis(leftAngle, Vector3.up) * transform.forward, Color.red);
            //Debug.DrawRay(constraints.head.lower.position, transform.rotation * Quaternion.AngleAxis(rightAngle, Vector3.up) * transform.forward, Color.blue);

            float averageAngle = Mathf.Clamp(rightAngle / 2 + leftAngle / 2, -150, 150);

            //Debug.DrawRay(constraints.head.lower.position, transform.rotation * Quaternion.AngleAxis(averageAngle , Vector3.up) * transform.forward, Color.green);

            constraints.head.upperBendAxis = Quaternion.AngleAxis(averageAngle / 2, Vector3.up) * calibration.lowerSpineAxis;
            constraints.head.UpdateOffsetsFromAxies();

            constraints.head.pole.position = constraints.waist.target.position + transform.rotation * Quaternion.AngleAxis(averageAngle, Vector3.up) * calibration.spinePoleOffset;
        } 

        public void UpdateIK()
        {
            if (!calibration.calibrated)
                return;
            
            state.isWalking = state.virtualVelocity != Vector3.zero;

            if(!fbt)
                PoseWaist();

            transform.position = new Vector3
            {
                x = ikRefs.waist.position.x,
                y = transform.parent.position.y,
                z = ikRefs.waist.position.z
            };

            if (!state.isWalking)
            {
                // Compensate for looking straght down with a lerp
                Vector3 headForward = ikRefs.head.forward;
                Vector3 headUp = ikRefs.head.up;
                float xRot = ikRefs.head.rotation.eulerAngles.x;
                if (xRot > 270)
                    headForward = Vector3.Lerp(-headUp, headForward,  (xRot - 270) / 90);
                else
                    headForward = Vector3.Lerp(headForward, headUp, xRot / 90);
                headForward.Normalize();
                
                float targetRotation = Quaternion.LookRotation(headForward).eulerAngles.y;
                float dAngle = Mathf.Abs(Mathf.DeltaAngle(targetRotation, state.rotation));
                if (dAngle > rotationBuffer)
                    state.rotation = Mathf.MoveTowardsAngle(state.rotation, targetRotation, dAngle - rotationBuffer); // My jank version of angle clamping
            }
            else
            {

                float targetRotation = Mathf.Atan2(state.virtualVelocity.x, state.virtualVelocity.z) * Mathf.Rad2Deg;
                float scaledSpeed = state.virtualVelocity.magnitude / state.walkAnimSpeed;
                if (Mathf.Abs(Mathf.DeltaAngle(ikRefs.head.eulerAngles.y, targetRotation)) > 100f)
                {
                    targetRotation += 180;
                    animator.SetFloat("speed", -scaledSpeed);
                }
                else
                    animator.SetFloat("speed", scaledSpeed);
                state.rotation = Mathf.MoveTowardsAngle(state.rotation, targetRotation, 360 * 2 * Time.deltaTime);
            }

            transform.rotation = Quaternion.Euler(
                transform.rotation.eulerAngles.x,
                state.rotation,
                transform.rotation.eulerAngles.z
            );

            animator.SetBool("walking", state.isWalking);

            if (!fbt)
            {
                RotateChest();
                PoseFeet();
            }
        }

        bool footPosRemembered = false;
        public void RememberAnimFootPos()
        {
            if (!state.isWalking || footPosRemembered)
                return;
            
            state.leftFoot = GetGlobal(constraints.leftFoot.end);
            state.rightFoot = GetGlobal(constraints.rightFoot.end);
            SetGlobalTransform(ikRefs.leftFoot, state.leftFoot);
            SetGlobalTransform(ikRefs.rightFoot, state.rightFoot);
            footPosRemembered = true;
        }

        private void LateUpdate()
        {
            RememberAnimFootPos();
            UpdateIK();
           
            executor.Execute();
            footPosRemembered = false;
        }
    }

    

}
public class CustomPlayableGraphEvaluator : MonoBehaviour
{

}
