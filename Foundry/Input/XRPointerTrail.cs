using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XRPointerTrail : MonoBehaviour
{
    private LineRenderer lr;

    void Start()
    {
        lr = transform.parent.GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (lr.positionCount > 0){
            transform.position = Vector3.Lerp(transform.position, lr.GetPosition(lr.positionCount - 1), 10 * Time.deltaTime);
        }
    }
}
