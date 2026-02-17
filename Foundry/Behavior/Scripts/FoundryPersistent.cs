using System;
using UnityEngine;

namespace Foundry
{
    public class FoundryPersistent : MonoBehaviour
    {
        public static FoundryPersistent Instance { get; private set; }
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
