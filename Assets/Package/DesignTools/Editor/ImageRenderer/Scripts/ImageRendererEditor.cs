using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ImageRenderer))]
public class ImageRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var t = (target as ImageRenderer);

        if (GUILayout.Button("Render Image"))
        {
            t.RenderImage();
        }
    }
}
