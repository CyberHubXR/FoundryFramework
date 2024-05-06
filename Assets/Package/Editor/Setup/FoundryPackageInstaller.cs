using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using CyberHub.Brane.Setup;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace Foundry.Core.Setup
{
    public class FoundryPackageInstaller : IModuleSetupTasks
    {
        bool installed = false;
        
        public FoundryPackageInstaller()
        {
            var installedPackageVersion = Resources.Load<FoundryVersion>("Version/FoundryInstalledVersion");
            var packageJsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Packages/com.cyberhub.foundry.core/package.json");

            // In our local dev environment, the package.json file is not in the expected location
            if (!packageJsonFile)
                packageJsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Package/package.json");
            
            var packageJson = JsonConvert.DeserializeObject<JObject>(packageJsonFile.text);
            var packageVersion = packageJson["version"]?.ToString() ?? "-1.0.0";
            installed = installedPackageVersion.version == packageVersion;
        }

        public IModuleSetupTasks.State GetTaskState() 
        {
            return installed ? IModuleSetupTasks.State.Completed : IModuleSetupTasks.State.UncompletedRequiredTasks;
        }

        public List<SetupTaskList> GetTasks()
        {
            //Find All Packages
            
            var installPackage = new SetupTask();
            installPackage.name = "Update or Install Local Foundry Assets";
            installPackage.SetTextDescription("We install all of our core prefabs and visual assets to your project's Assets folder so you can edit or replace them if needed.");
            installPackage.action = new SetupAction
            {
                name = "Install Package",
                callback = () =>
                {
                    AssetDatabase.ImportPackage("Packages/com.cyberhub.foundry.core/FoundryAssets.unitypackage", true);
                }
            };

            var installTaskList = new SetupTaskList("Install");
            installTaskList.Add(installPackage);

            return new List<SetupTaskList> { installTaskList };
        }

        public string ModuleName()
        {
            return "Foundry Package Installer";
        }

        public string ModuleSource()
        {
            return "foundry.core";
        }
    }
}