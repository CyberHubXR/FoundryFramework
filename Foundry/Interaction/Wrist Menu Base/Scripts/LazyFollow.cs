using UnityEngine;

public class LazyFollow : MonoBehaviour
{
    // The target to follow (e.g., your VR camera)
    public Transform target;
    
    // How fast the UI follows the target
    public float followSpeed = 2.0f;
    
    // Offset relative to the target's position (e.g., to keep UI in a comfortable view)
    public Vector3 offset = new Vector3(0, 1, 0);

    void LateUpdate()
    {
        if (target != null)
        {
            // Determine the desired position with the offset applied
            Vector3 desiredPosition = target.TransformPoint(offset);
            
            // Smoothly interpolate from the current position to the desired position
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
            
            // Optionally, orient the UI so it faces the target.
            // Uncomment the next line if you want the UI to always look at the target.
            // its flipped i need to to look the other way
            // transform.LookAt(target);
            transform.LookAt(2 * transform.position - target.position);
        }
    }
}
