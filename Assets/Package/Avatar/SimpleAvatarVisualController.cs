using UnityEngine;

namespace Foundry
{
    public class SimpleAvatarVisualController : MonoBehaviour
    {
            
        [Header("Hair")]
        public GameObject[] hairOptions;

        private int currentHairIndex = 0;

        [Header("Color Variants")]
        [SerializeField] private Renderer[] renderersToModify;
        [SerializeField] private Texture[] baseMaps;

        private int currentColorIndex = 0;
        private Material runtimeMaterial;


        private void Start()
        {
            ApplyHair(currentHairIndex);

            if (renderersToModify.Length > 0)
            {
                runtimeMaterial = new Material(renderersToModify[0].material);

                foreach (var r in renderersToModify)
                {
                    r.material = runtimeMaterial;
                }

                ApplyColor(currentColorIndex);
            }
        }

        public void NextHair()
        {
            currentHairIndex++;
            if (currentHairIndex >= hairOptions.Length)
                currentHairIndex = 0;

            ApplyHair(currentHairIndex);
        }

        public void PreviousHair()
        {
            currentHairIndex--;
            if (currentHairIndex < 0)
                currentHairIndex = hairOptions.Length - 1;

            ApplyHair(currentHairIndex);
        }

        public void NextColor()
        {
            currentColorIndex++;
            if (currentColorIndex >= baseMaps.Length)
                currentColorIndex = 0;

            ApplyColor(currentColorIndex);
        }

        public void PreviousColor()
        {
            currentColorIndex--;
            if (currentColorIndex < 0)
                currentColorIndex = baseMaps.Length - 1;

            ApplyColor(currentColorIndex);
        }

        private void ApplyColor(int index)
        {
            if (runtimeMaterial != null)
            {
                runtimeMaterial.SetTexture("_BaseMap", baseMaps[index]);
            }
        }

        private void ApplyHair(int index)
        {
            for (int i = 0; i < hairOptions.Length; i++)
            {
                hairOptions[i].SetActive(i == index);
            }
        }

        public int GetCurrentHairIndex()
        {
            return currentHairIndex;
        }



    }
}