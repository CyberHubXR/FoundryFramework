using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    public class GenerateDemoShape : MonoBehaviour
    {
        public GameObject[] shapes;

        public float maxDistance = 10f;
        public Vector3 startScale;
        public AnimationCurve scaleDistanceCurve;
        public AnimationCurve scaleHeightDistanceCurve;
        public AnimationCurve relativeOffsetCurve;

        [ContextMenu("Generate")]
        public void ChangeShape() {
            for (int i = 0; i < shapes.Length; i++) {
                shapes[i].transform.localScale = startScale * scaleDistanceCurve.Evaluate(shapes[i].transform.position.magnitude/maxDistance) +
                    relativeOffsetCurve.Evaluate(Random.value)*new Vector3(Random.value, Random.value, Random.value)
                    + startScale.y * scaleHeightDistanceCurve.Evaluate(shapes[i].transform.position.magnitude/maxDistance) * Vector3.up
                +startScale.y * relativeOffsetCurve.Evaluate(shapes[i].transform.position.magnitude/maxDistance) * Vector3.up * Random.value;


            }
        }
    }
}
