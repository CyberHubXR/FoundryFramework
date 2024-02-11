using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class NetworkVoiceOutput : MonoBehaviour
    {
        
        [HideInInspector]
        public MonoBehaviour nativeScript;
        
    }
}
