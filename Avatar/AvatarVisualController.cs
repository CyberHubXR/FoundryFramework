using UnityEngine;

namespace Foundry
{
    public class AvatarVisualController : MonoBehaviour
    {
        [Header("Hair Options")]
        [Tooltip("All available hair meshes. Exactly one will be enabled.")]
        public MeshRenderer[] hairOptions;

        /// <summary>
        /// Apply a full avatar config to the visual meshes.
        /// </summary>
        
        void Start()
        {
            Debug.Log("Start called");
            ApplyConfig(new AvatarConfig { hair = 1 });
        }
        
        public void ApplyConfig(AvatarConfig config)
        {
            ApplyHair(config.hair);
            Debug.Log("Applied Config");
        }

        void ApplyHair(int hairIndex)
        {
            if (hairOptions == null || hairOptions.Length == 0)
                return;

            for (int i = 0; i < hairOptions.Length; i++)
            {
                if (hairOptions[i] == null)
                    continue;

                hairOptions[i].enabled = (i == hairIndex);
            }
        }
    }
}