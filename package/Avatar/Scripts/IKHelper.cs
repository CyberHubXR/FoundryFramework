using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKHelper : MonoBehaviour
{
    [System.Serializable]
    public struct Bones
    {
         public Transform hip;
         public Transform spine;
         public Transform spine1;
         public Transform spine2;
         public Transform neck;
         public Transform head;
         public Transform leftArm;
         public Transform leftForeArm;
         public Transform leftHand;
         public Transform rightArm;
         public Transform rightForeArm;
         public Transform rightHand;
         public Transform leftLeg;
         public Transform leftKnee;
         public Transform leftFoot;
         public Transform rightLeg;
         public Transform rightKnee;
         public Transform rightFoot;
    }
}
