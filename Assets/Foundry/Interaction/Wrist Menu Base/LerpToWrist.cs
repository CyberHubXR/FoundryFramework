using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LerpToWrist : MonoBehaviour
{
    public float lerpTime;

    public Transform target;

    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, target.position, lerpTime * Time.deltaTime);
    }
}
