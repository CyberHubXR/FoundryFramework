using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    public class IKTransformConstraint : IKTargetedConstraint
    {
        public bool localTransform = false;
        public bool onlyTrackActive = true;
        public override void Execute()
        {
            if (weight == 0)
                return;
            var t = transform;
            
            if (onlyTrackActive && !target.gameObject.activeInHierarchy)
                return;
            
            if (localTransform)
            {
                t.localPosition = target.localPosition;
                t.localRotation = target.localRotation;
                return;
            }

            t.position = target.position;
            t.rotation = target.rotation;
        }
    }
}

