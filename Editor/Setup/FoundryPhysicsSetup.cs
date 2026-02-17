using System.Collections.Generic;
using CyberHub.Foundry.Setup;
using UnityEditor;
using UnityEngine;


namespace Foundry.Core.Setup { 
    public class FoundryPhysicsSetup: IModuleSetupTasks
    {
        bool layersExist = false;
        
        public FoundryPhysicsSetup()
        {
            layersExist = FoundryPhysicsSetter.LayersExist();
        }
        
        public IModuleSetupTasks.State GetTaskState()
        {
            return layersExist ? IModuleSetupTasks.State.Completed : IModuleSetupTasks.State.UncompletedRequiredTasks;
        }

        public List<SetupTaskList> GetTasks()
        {
            if (layersExist)
                return new();
            var configureLayersTask = new SetupTask();
            configureLayersTask.name = "Required Physics Layers";
            configureLayersTask.SetTextDescription("We require three custom physics layers:\nFoundryPlayer,\nFoundryHand,\nFoundryGrabbable.\nThese layers help with stability and performance");
            configureLayersTask.action = new SetupAction
            {
                name = "Add Layers",
                callback = InstallPackage
            };
            
            var configureLayersTaskList = new SetupTaskList("Settings");
            configureLayersTaskList.Add(configureLayersTask);
            
            return new List<SetupTaskList>{ configureLayersTaskList};
        }

        public string ModuleName()
        {
            return "Foundry Core Physics";
        }

        public string ModuleSource()
        {
            return "foundry.framework";
        }

        static void InstallPackage() {
            FoundryPhysicsSetter.CreateLayers();
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer("FoundryPlayer"), LayerMask.NameToLayer("FoundryHand"), true);
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer("FoundryPlayer"), LayerMask.NameToLayer("FoundryGrabbable"), true);

            AssetDatabase.Refresh(); 
            EditorUtility.RequestScriptReload();
        }
    }
}