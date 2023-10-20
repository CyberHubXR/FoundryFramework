using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

namespace Foundry
{
    [RequireComponent(typeof(FullBodyAvatar)), RequireComponent(typeof(HumanoidIK))]
    public class FullBodyAvatarNetworking : NetworkBehaviour
    {
        private HumanoidIK ik;
        private FullBodyAvatar avatar;


        public void Start()
        {
            ik = GetComponent<HumanoidIK>();
            avatar = GetComponent<FullBodyAvatar>();
        }

        public override void FixedUpdateNetwork()
        {
        }
    }
}
