using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace Foundry.Core.Setup
{
    public class FoundryPackageInstaller : IModuleSetupTasks
    {
        bool installed;

        static string installedVersion;
        static FoundryVersion installedPackageVersion;

        static ListRequest request;

        static void CompareVersionNumbers()
        {
            //Find All Packages
            request = Client.List();
            EditorApplication.update += Progress;
        }

        static void Progress() 
        {
            installedPackageVersion = Resources.Load<FoundryVersion>("Version/FoundryInstalledVersion");
            
            if (request.IsCompleted) 
            {
                if (request.Status == StatusCode.Success)
                {
                    foreach (var package in request.Result)
                    {   
                        if (package.name == "com.cyberhub.foundry.core") 
                        {              
                            if (installedPackageVersion.version != package.version)
                            {
                                AssetDatabase.ImportPackage("Packages/com.cyberhub.foundry.core/FoundryPackage.unitypackage", false);

                                installedPackageVersion.version = package.version;
                            }
                            else 
                            {
                                Debug.LogWarning($"Installed {package.name} version : {package.version} is greater or the same than current. Your all set (import abandoned)");
                            }
                        }
                    }
                }
                else if (request.Status >= StatusCode.Failure)
                {
                    Debug.Log(request.Error.message);
                }

                EditorApplication.update -= Progress;
            }
        }

        public IModuleSetupTasks.State GetTaskState()
        {
            return installed ? IModuleSetupTasks.State.Completed : IModuleSetupTasks.State.UncompletedRequiredTasks;
        }

        public List<SetupTaskList> GetTasks()
        {
            //Find All Packages
            request = Client.List();
            EditorApplication.update += Progress;

            var installPackage = new SetupTask();
            installPackage.name = "Update Installed Package";
            installPackage.SetTextDescription("If the auto update fails");
            
            installPackage.action = new SetupAction
            {
                name = "Check For Package Updates",
                callback = CompareVersionNumbers
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