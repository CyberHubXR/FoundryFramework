using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

namespace Foundry
{
    public class FullBodyAvatar : Avatar
    {
        public HumanoidIK ik;

        [Header("UserHeightCalibration")]
        [Range(1.5F, 2F)]
        public float avatarHeight;
        public GameObject avatarHolder;
        
        public Animator animator;

        public override void Start()
        {
            base.Start();
            CalibrateAvatarHeight();
        }

        public void SetAnimatorAvatar(UnityEngine.Avatar avatar)
        {
            animator.avatar = avatar;
        }

        public void CalibrateRig(Animator avatarAnimator)
        {
            ik.Calibrate(avatarAnimator, true);
        }

        void CalibrateAvatarHeight()
        {
            // Turning this off until we have a manual way to set or calibrate player height, only going to cause bugs until then - Eli
            return;


            float headHeight = ik.ikRefs.head.position.y;
            float scale = headHeight / avatarHeight;
            float safeScale = Mathf.Clamp(scale, 0.8F, 2F);
            avatarHolder.transform.localScale = Vector3.one * safeScale;
        }

        public override void SetTrackingMode(TrackingMode mode)
        {
            base.SetTrackingMode(mode);
            switch (mode)
            {
                case TrackingMode.OnePoint:
                    ik.constraints.leftHand.weight = 0;
                    ik.constraints.rightHand.weight = 0;
                    break;
                case TrackingMode.ThreePoint:
                    ik.constraints.leftHand.weight = 1;
                    ik.constraints.rightHand.weight = 1;
                    break;
                case TrackingMode.SixPoint:
                    ik.constraints.leftHand.weight = 1;
                    ik.constraints.rightHand.weight = 1;
                    break;
            }
        }
        
        private void Update()
        {
            UpdateConstraint(ik.constraints.leftHand, player.trackers.leftHand.gameObject.activeInHierarchy);
            UpdateConstraint(ik.constraints.rightHand, player.trackers.rightHand.gameObject.activeInHierarchy);
        }
        
        public void UpdateConstraint(IKConstraint constraint, bool active)
        {
            constraint.weight = Mathf.MoveTowards(constraint.weight, active ? 1 : 0, Time.deltaTime * 5);
        }

        public override void SetVirtualVelocity(Vector3 velocity)
        {
            ik.virtualVelocity = velocity;
        }
    }
}

