using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageRenderer : MonoBehaviour
{
    [Tooltip("Total Scale")] [Range(1, 10)] public float scaleMultiplier = 1;

    [Tooltip("The image you want to diplay")] public Texture2D image;

    private GameObject imageRendered;
    private GameObject imageFrameInstance;

    private const string imageFramePath = "ImageFrame";
    
    public void RenderImage()
    {
        transform.localScale = Vector3.one;
        
        if (imageRendered == null && imageFrameInstance == null)
        {
            GameObject imagePanel = GameObject.CreatePrimitive(PrimitiveType.Plane);
            imagePanel.transform.localScale = new Vector3(image.width / 10000F, 1, image.height / 10000F);
            imagePanel.transform.position = new Vector3(0,0, 0.025F);
            imagePanel.transform.Rotate(90, 0, 0);
            imagePanel.transform.SetParent(transform);

            GameObject ImageFrame = Instantiate(Resources.Load(imageFramePath) as GameObject, transform);
            ImageFrame.transform.localScale = new Vector3(image.width / 1000F, 1, image.height / 1000F);

            Material imageRenderedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            imageRenderedMaterial.mainTexture = image;
            
            imagePanel.GetComponent<Renderer>().sharedMaterial = imageRenderedMaterial;

            imageRendered = imagePanel;
            imageFrameInstance = ImageFrame;
        }
        else
        {
            DestroyImmediate(imageRendered);
            DestroyImmediate(imageFrameInstance);
            
            GameObject imagePanel = GameObject.CreatePrimitive(PrimitiveType.Plane);
            imagePanel.transform.localScale = new Vector3(image.width / 10000F, 1, image.height / 10000F);
            imagePanel.transform.position = new Vector3(0,0, 0.025F);
            imagePanel.transform.Rotate(90, 0, 0);
            imagePanel.transform.SetParent(transform);
            
            GameObject ImageFrame = Instantiate(Resources.Load(imageFramePath) as GameObject, transform);
            ImageFrame.transform.localScale = ImageFrame.transform.localScale = new Vector3(image.width / 1000F, 1, image.height / 1000F);

            Material imageRenderedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            imageRenderedMaterial.mainTexture = image;
            
            imagePanel.GetComponent<Renderer>().sharedMaterial = imageRenderedMaterial;

            imageRendered = imagePanel;
            imageFrameInstance = ImageFrame;
        }
        
        transform.localScale = Vector3.one * scaleMultiplier; 
    }
}
