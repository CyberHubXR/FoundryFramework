using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoneRenderer : MonoBehaviour
{
    public Color rigColor = new(1,0,0,1);

    void OnDrawGizmos()
    {
        Gizmos.color = rigColor;
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            Gizmos.DrawLine(child.position, child.parent.position);
        }
    }
}
