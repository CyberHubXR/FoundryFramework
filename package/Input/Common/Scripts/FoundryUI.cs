using UnityEngine;

namespace Foundry
{
    /// <summary>
    /// A component that sets up a canvas to be used with Foundry.
    /// </summary>
    public class FoundryUI : MonoBehaviour
    {
        void Start()
        {
            Canvas canvas;
            if(!TryGetComponent(out canvas))
            {
                Debug.LogWarning("Foundry UI component is attached to a GameObject without a Canvas component.");
                return;
            }

            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                if (!FoundryUIInput.current)
                {
                    Debug.LogWarning("For a canvas to receive input there must be an event system with a FoundryUIInput script in the scene.");
                    return;
                }
                canvas.worldCamera = FoundryUIInput.current.canvasCamera;
            }
        }
    }
}
