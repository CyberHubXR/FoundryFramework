using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using CyberHub.Brane.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Foundry.Core.Editor
{
    [FilePath("ProjectSettings/Packages/Foundry/PackageRefState.asset", FilePathAttribute.Location.ProjectFolder)]
    public class FoundryPackageRefState : ScriptableSingleton<FoundryPackageRefState>
    {
        [Serializable]
        public class PackageState
        {
            public string name;
            public List<string> sources;
        }
        
        


        [SerializeField] List<PackageState> storedPackageStates = new();

        public static PackageState GetStoredPackageState(string name)
        {
            foreach (var package in instance.storedPackageStates)
            {
                if (package.name == name)
                    return package;

            }
            return null;
        }

        public static void TrackPackage(string name)
        {
            PackageState newState = new PackageState();
            newState.name = name;

            string packageRef = PackageManagerUtil.GetPackageSource(name);
            
        }
    }

    /// <summary>
    /// In-progress internal tool for switching between using local cloned packages or using packages from the registry.
    /// </summary>
    public class FoundryDevToolsWindow : EditorWindow
    {
    
        //[MenuItem("Window/Foundry/Foundry Dev Tools", false, 2)]
        public static void OpenWindow()
        {
            var windowInstance = GetWindow<FoundryDevToolsWindow>(false, "Foundry Dev Tools");
        }
    
        public void CreateGUI()
        {
            StyleLength headerFontSize = new StyleLength(16f);
            StyleLength fontSize = new StyleLength(15f);

            VisualElement root = rootVisualElement;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;

            //Logo
            var logoData = Resources.Load<Texture2D>("foundry_icon");
            var logo = new Image();

            logo.image = logoData;
            logo.style.maxHeight = 150;
            logo.style.alignSelf = Align.Center;
            logo.style.marginTop = 20;
            logo.style.marginBottom = 20;
            root.Add(logo);

            var packageRefMode = new Foldout();
        }
    }
}
