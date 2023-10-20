using Foundry.Core.Editor;
using System.Collections;
using System.Collections.Generic;
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
            configureLayersTaskList.tasks = new List<SetupTask>();
            
            return new List<SetupTaskList>{ configureLayersTaskList};
        }

        public string ModuleName()
        {
            return "Foundry Core Physics";
        }

        public string ModuleSource()
        {
            return "foundry.core";
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