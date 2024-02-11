using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
#endif

namespace Foundry
{
    public abstract class FoundryScript : MonoBehaviour
    {
        
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(FoundryScript), true)]
    public class FoundryScriptEditor : Editor
    {
        Texture2D header;
        float headerScale = 0.1f;
        float bottomPadding = 7f;
        float leftPadding = 150f;

        public void OnEnable()
        {
            header = Resources.Load<Texture2D>("FoundryHeader");
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var headerImage = new Image();
            headerImage.image = header;
            headerImage.style.alignSelf = Align.Stretch;
            headerImage.style.maxHeight = 60;
            root.Add(headerImage);
            
            string name = target.GetType().Name;
            bool lastWasLower = false;
            for (int i = 1; i < name.Length; i++)
            {
                if (lastWasLower && 'A' <= name[i] && name[i] <= 'B')
                    name.Insert(i, " ");
                lastWasLower = 'a' <= name[i] && name[i] <= 'z';
            }

            var scriptTitle = new Button();
            scriptTitle.Add(new Label(name));
            scriptTitle.clicked += () =>
            {
                var script = serializedObject.FindProperty("m_Script").objectReferenceValue;
                EditorGUIUtility.PingObject(script);
            };
            root.Add(scriptTitle);
            
            
            root.Add(new IMGUIContainer(() =>
            {
                DrawPropertiesExcluding(serializedObject, new string[] { "m_Script" });
                serializedObject.ApplyModifiedProperties();
            }));
            return root;
        }
    }
    
    #endif
}
