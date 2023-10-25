# Setup Wizard
The setup wizard is Foundry's way of helping you get started with your project. If there's some dependencies or 
configuration that you're missing, the window will open and notify you of what you need to do. 

## Adding a setup task
If you are developing a module or project using foundry and you want to add your own sections and tasks to this window, you can!

### Adding a task
The most common implementation of setup tasks is checking if a needed dependency is installed, so let's look at an example of that.

```csharp
using Foundry.Core.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace Foundry.Core.Setup { 
    // Implement IModuleSetupTasks, the setup wizard will then be able to find this class and add it to the list of tasks.
    public class FoundryPhysicsSetup: IModuleSetupTasks
    {
        // Cache state since this gets reloaded every time assemblies are reloaded
        bool layersExist = false;
        
        // Cache stuff in the constructor
        public FoundryPhysicsSetup()
        {
            layersExist = FoundryPhysicsSetter.LayersExist();
        }
        
        // Return the current state of all the tasks we register from this class, if you return UncompletedRequiredTasks the setup window will open.
        public IModuleSetupTasks.State GetTaskState()
        {
            return layersExist ? IModuleSetupTasks.State.Completed : IModuleSetupTasks.State.UncompletedRequiredTasks;
        }

        // Return a list of task lists, each task list will be a section in the setup window, all grouped under one module.
        public List<SetupTaskList> GetTasks()
        {
            if (layersExist)
                return new();
            var configureLayersTask = new SetupTask();
            configureLayersTask.name = "Required Physics Layers";
            configureLayersTask.SetTextDescription("We require three custom physics layers:\nFoundryPlayer,\nFoundryHand,\nFoundryGrabbable.\nThese layers help with stability and performance");
            
            // Add an action to be completed when clicked.
            configureLayersTask.action = new SetupAction
            {
                name = "Add Layers",
                callback = AddLayers
            };
            
            // Create a "settings" list and add our task to it.
            var configureLayersTaskList = new SetupTaskList("Settings");
            configureLayersTaskList.Add(configureLayersTask);
            
            // This is built to be able to return multiple task lists (for example if you wanted to have a "Dependencies" and "Settings" section) for now we just do one.
            return new List<SetupTaskList>{ configureLayersTaskList};
        }

        // Return the name and source of the module that this task is for, this is only used for displaying in the setup wizard 
        // so you could put anything here, but keeping to a pattern is nice.
        public string ModuleName()
        {
            return "Foundry Core Physics";
        }

        public string ModuleSource()
        {
            return "foundry.core";
        }

        // In this example we add phsyics layers, but you could do anything here.
        static void AddLayers() {
            FoundryPhysicsSetter.CreateLayers();
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer("FoundryPlayer"), LayerMask.NameToLayer("FoundryHand"), true);
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer("FoundryPlayer"), LayerMask.NameToLayer("FoundryGrabbable"), true);

            AssetDatabase.Refresh(); 
            EditorUtility.RequestScriptReload();
        }
    }
}
```

This creates a task that will check if the package com.example.package is installed, and if it isn't, it will add a 
task to the setup wizard that will install it.

It is worth noting that the you can populate the description of a task with any VisualElement that you want. Here
we just use the `SetTextDescription` method to set the text, but you could also do it manually like so:

```csharp
// with a label
addPackageTask.description = new Label("We need to add com.example.package to the project for it to work.");

// with a text element (this is what SetTextDescription does)
var desc = new TextElement();
desc.text = "We need to add com.example.package to the project for it to work.";
desc.style.whiteSpace = WhiteSpace.Normal;
addPackageTask.description = desc;
```

Why would you ever use a custom description? Well what if you wanted to have an image instead of text? You could do that!

```csharp
var descImage = new Image();
descImage.image = Resources.Load<Texture2D>("description_image");
descImage.style.maxHeight = 150;
descImage.style.alignSelf = Align.Center;

addPackageTask.description = descImage;
```

You could even do something interactive, but that's beyond the scope of this example.