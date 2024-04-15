using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(VideoResizer))]
public class VideoResizerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var t = (target as VideoResizer);

        if (GUILayout.Button("Resize Screen"))
        {
            t.ResizeScreen();
        }
    }
}
