# Setup Wizard
The setup wizard is Foundry's way of helping you get started with your project. If there's some dependencies or 
configuration that you're missing, the window will open and notify you of what you need to do. 

## Adding a setup task
If you are developing a module or project using foundry and you want to add your own sections and tasks to this window, you can!

### Adding a task
The most common implementation of setup tasks is checking if a needed dependency is installed, so let's look at an example of that.

```csharp
using Foundry.Core.Editor;
using Foundry.Core.Setup;
using UnityEditor;
using UnityEngine;

public class MyDependencyInstaller
{
    /* Initialize on load will run this method when the editor is opened, 
     * and when scripts are reloaded. If there is a task to be added 
     * this is when we want to do it.
     */
    [InitializeOnLoadMethod]
    static void RegisterTasks()
    {
        // PackageManagerUtil is a helper class provided by Foundry to make working with the package manager easier.
        if (PackageManagerUtil.IsPackageInstalled("com.example.package"))
            return;

        SetupTask addPackageTask = new SetupTask();
        addPackageTask.name = "Add Required Package";
        addPackageTask.SetTextDescription("We need to add com.example.package to the project for it to work.");
        addPackageTask.action = new SetupAction
        {
            name = "Install", // Text shown on button
            callback = InstallPackage // callback to run when button is clicked
        };
        
        /* Add the task to the setup wizard. We don't need to worry about 
         * removing it later since tasks need to be added every time the 
         * editor reloads to persist.
         */
        FoundrySetupWizard.AddRequiredDependency(addPackageTask);
    }

    static void InstallPackage()
    {
        Debug.Log("Installing my package.");
        PackageManagerUtil.AddPackage("com.example.package", "https://github.com/mygithub/myrepo.git#v1.0.0");
        PackageManagerUtil.Apply();
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

### Custom Sections
Due to the unpredictable call order of `[InitializeOnLoadMethod]` calls, custom sections do not have a `CreateSection` 
api, instead any method that refers to a section that doesn't exist will create it for you. So feel free to call the 
methods below in any order. Note that this makes it super important that you don't have any typos in your section names.

If you want to add a task to a new section other than the default "Required Dependencies" section that 
`AddRequiredDependency` appends too. It's as easy as using the generic `AddTask` method instead.

```csharp
FoundrySetupWizard.AddTask("My Custom Section", setupTask);
```

If you want to change the order of the sections, it can be done with the `Required Settings` method. 
By default all sections are added with a priority of 0, so to ensure that your section is further towards the bottom of the list, 
give it a higher priority. Or give it a negative priority to move it towards the top.

```csharp
FoundrySetupWizard.SetTaskListPlacement("Bottom of the list. Probbably.", 9000);
FoundrySetupWizard.SetTaskListPlacement("Top of the list!!!", -9000);
```