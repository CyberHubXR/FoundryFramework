using System.Collections;
using UnityEngine;

namespace Foundry
{
    public class PlayerMenuAnimator : MonoBehaviour
    {
        public static PlayerMenuAnimator Instance { get; private set; }
        public AnimationCurve width;
        public AnimationCurve height;
        public float spawnTime = 1;

        public GameObject visualRoot;
        public float Weight { get; private set; } = 0;

        enum AnimationState
        {
            Idle,
            Opening,
            Closing
        }

        private AnimationState state = AnimationState.Idle;
        private Transform spawnTarget;
        private Transform lookTarget;

        private Coroutine animationRoutine;

        public void ToggleMenu(Transform spawnTarget, Transform lookTarget)
        {
            // if we have sent a spawn target in the spawn use that, otherwise use the default
            this.spawnTarget = spawnTarget != null ? spawnTarget : this.spawnTarget;
            this.lookTarget = lookTarget;
            
            if(animationRoutine != null)
            {
                state = state == AnimationState.Closing ? AnimationState.Opening : AnimationState.Closing;
                return;
            }
            
            state = Weight <= 0 ? AnimationState.Opening : AnimationState.Closing;
            animationRoutine = StartCoroutine(AnimateMenu());
        }

        public void OnEnable()
        {
            if(Instance != null && Instance != this)
            {
                Debug.LogWarning("Two instances of PlayerMenuAnimator detected! Make sure only one is enabled at at time!");
                return;
            }
            Instance = this;
            transform.localScale = new Vector3(0, 0, 1);
            
            if(Weight <= 0)
                visualRoot.SetActive(false);
            
            if(state != AnimationState.Idle)
                animationRoutine = StartCoroutine(AnimateMenu());
        }

        public void OnDisable()
        {
            if (Instance == this)
                Instance = null;
            
            if(animationRoutine != null)
                StopCoroutine(animationRoutine);
        }

        IEnumerator AnimateMenu()
        {
            if (state == AnimationState.Opening)
            {
                visualRoot.SetActive(true);
                transform.position = spawnTarget.position;
                //transform.rotation = Quaternion.LookRotation(lookTarget.position - visualRoot.transform.position, Vector3.up);
            }
            
            while (state != AnimationState.Idle)
            {
                float direction = 0;
                switch (state)
                {
                    case AnimationState.Closing:
                        if (Weight <= 0)
                        {
                            state = AnimationState.Idle;
                            continue;
                        }
                        direction = -1;
                        break;
                    case AnimationState.Opening:
                        if (Weight >= 1)
                        {
                            state = AnimationState.Idle;
                            continue;
                        }
                        direction = 1;
                        break;
                }
                float step = (1f / spawnTime) * Time.deltaTime * direction;
                Weight = Mathf.Clamp01(step + Weight);

                float lerpWeight = state == AnimationState.Closing ? 1 - Weight : Weight; 
                transform.position = Vector3.Lerp(transform.position, spawnTarget.position, lerpWeight);
                //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation( lookTarget.position - visualRoot.transform.position, Vector3.up), lerpWeight);
                
                transform.localScale = new Vector3(width.Evaluate(Weight), height.Evaluate(Weight), 1);

                yield return null;
            }
            
            visualRoot.SetActive(Weight > 0);
            
            animationRoutine = null;
        }
    }
}
