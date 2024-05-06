using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Foundry.DesignTools
{
    public class RenderDebugHUD : MonoBehaviour
    {
        public bool showDebugHUD = true;
        public TMP_Text text;
        public InputActionProperty toggleAction;
        [Tooltip("How often to update the text in updates per second")]
        [Range(16, 144)]
        public float updateRate = 64f;
        
        private Coroutine updateTextCoroutine;
        
        // Start is called before the first frame update
        void Awake()
        {
            // Only show this object if we're in the editor and it's enabled
            #if UNITY_EDITOR
            gameObject.SetActive(showDebugHUD);
            if (showDebugHUD)
                updateTextCoroutine = StartCoroutine(UpdateText());
            toggleAction.action.Enable();
            toggleAction.action.performed += ctx =>
            {
                showDebugHUD = !showDebugHUD;
                
                gameObject.SetActive(showDebugHUD);
                if (showDebugHUD)
                    updateTextCoroutine = StartCoroutine(UpdateText());
                else
                    StopCoroutine(updateTextCoroutine);
            };
            
            #else
            Destroy(gameObject);
            #endif
        }
#if UNITY_EDITOR

        IEnumerator UpdateText()
        {
            while (true)
            {
                text.text = $@"FPS: {1 / Time.deltaTime}
Frame Time: {UnityEditor.UnityStats.frameTime}
Render Time: {UnityEditor.UnityStats.renderTime}
Tris: {UnityEditor.UnityStats.triangles}
Verts: {UnityEditor.UnityStats.vertices}
Draw Calls: {UnityEditor.UnityStats.drawCalls}
Batches: {UnityEditor.UnityStats.batches}";
                yield return new WaitForSecondsRealtime(1f / updateRate);
            }

            yield return null;
        }
#endif
    }
}

