using System;
using UnityEngine;

namespace Foundry
{
    /// <summary>
    /// A behavior for controlling an objects lifetime.
    /// </summary>
    public class ObjectLifetime : MonoBehaviour
    {
        #region Unity Inspector Variables
        [SerializeField]
        [Tooltip("Whether GameObject.DontDestroyOnLoad should be called to keep the object alive across scenes.")]
        private bool dontDestroyOnLoad = true;
        #endregion // Unity Inspector Variables

        #region Unity Message Handlers
        /// <inheritdoc/>
        private void Awake()
        {
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        #endregion // Unity Message Handlers
    }
}
