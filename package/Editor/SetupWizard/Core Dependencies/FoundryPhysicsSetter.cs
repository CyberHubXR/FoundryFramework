using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FoundryPhysicsSetter : Editor
{
    static string[] layerNames = new string[] { 
        "FoundryPlayer",
        "FoundryGrabbable",
        "FoundryHand"
    };


    public static void CreateLayers() {
        foreach(var layer in layerNames) {
            CreateLayer(layer);
        }
    }

    public static bool LayersExist() {
        bool success = true;
        Dictionary<string, int> existingLayers = GetAllLayers();
        foreach(var layer in layerNames) {
            if(!existingLayers.ContainsKey(layer)) {
                success = false; 
                break;
            }
        }
        return success;
    }


    static void CreateLayer(string name) {
        bool success = false;
        Dictionary<string, int> existingLayers = GetAllLayers();

        if(!existingLayers.ContainsKey(name)) {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            for(int i = 0; i < 31; i++) {
                SerializedProperty element = layers.GetArrayElementAtIndex(i);
                if(string.IsNullOrEmpty(element.stringValue) && i >= 6) {
                    element.stringValue = name;

                    tagManager.ApplyModifiedProperties();
                    success = true;
                    Debug.Log(i.ToString() + " layer created: " + name);
                    break;
                }
            }

            if(!success) {
                Debug.Log("Could not create layer, you likely do not have enough empty layers. Please delete an unused layer");
            }
        }
    }

    public static Dictionary<string, int> GetAllLayers() {
        Dictionary<string, int> layerDictionary = new Dictionary<string, int>();
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        int layerSize = layers.arraySize;

        for(int i = 0; i < layerSize; i++) {
            SerializedProperty element = layers.GetArrayElementAtIndex(i);
            string layerName = element.stringValue;

            if(!string.IsNullOrEmpty(layerName)) {
                layerDictionary.Add(layerName, i);
            }
        }

        return layerDictionary;
    }

}
