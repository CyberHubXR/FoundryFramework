using System;
using System.Collections.Generic;
using Foundry.Core.Editor;
using Foundry.Core.Editor.UIUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Foundry.Core.Setup
{
    /// <summary>
    /// Represents an action to complete a SetupTask, can be automatic, or guide the user to documentation as needed
    /// </summary>
    public class SetupAction
    {
        public string name;
        public Action callback;
        
        /// <summary>
        /// Create an action that opens a link to documentation
        /// </summary>
        /// <param name="url">Url to open</param>
        /// <returns></returns>
        public static SetupAction OpenDocLink(string url)
        {
            return new SetupAction
            {
                name = "Open Docs",
                callback = () =>
                {
                    Application.OpenURL(url);
                }
            };
        }
    }
    
    /// <summary>
    /// Represents a task that is required or suggested to be completed to set up a foundry project
    /// </summary>
    public class SetupTask
    {
        public string name = "new task";

        public enum Urgency
        {
            Required,
            Suggested
        }

        /// <summary>
        /// If a task is required, it will force the setup wizard to open, while a suggested task will not.
        /// </summary>
        public Urgency urgency = Urgency.Required;

        /// <summary>
        /// If defined, will be rendered between name and the setup action button
        /// </summary>
        public VisualElement description;
        
        /// <summary>
        /// If this is not null, it will be represented as a button that uses the name and action defined in the SetupAction
        /// </summary>
        public SetupAction action;

        /// <summary>
        /// Helper for creating a text description without needing to interact with the Unity UI interface
        /// </summary>
        /// <param name="text"></param>
        public void SetTextDescription(string text)
        {
            var desc = new TextElement();
            desc.text = text;
            desc.style.whiteSpace = WhiteSpace.Normal;

            description = desc;
        }
    }

    /// <summary>
    /// Organizational unit for a list of tasks. (Required Dependencies, Required Settings, etc)
    /// </summary>
    public class SetupTaskList
    {
        public string name;

        /// <summary>
        /// If defined, this will be displayed under name before tasks. It is ok to have this null most of the time.
        /// </summary>
        public VisualElement description;

        /// <summary>
        /// Tasks to display in this list
        /// </summary>
        public List<SetupTask> tasks = new();

        public SetupTaskList(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Add a task to this list
        /// </summary>
        /// <param name="task">Task to add</param>
        public void Add(SetupTask task)
        {
            tasks.Add(task);
        }
    }

    public class FoundrySetupWizard : EditorWindow
    {
        [MenuItem("Foundry/Setup Wizard", false, 1)]
        public static void OpenWindow()
        {
            var window = GetWindow<FoundrySetupWizard>(false, "Setup Wizard", true);
            window.Repaint();
        }
        
        /// <summary>
        /// Modules with tasks to complete
        /// </summary>
        private static List<IModuleSetupTasks> modules = new();
        
        /// <summary>
        /// Check if there are any required tasks, if so force this window to open.
        /// </summary>
        private static void CheckForTasks()
        {
            var modulesTypes = ClassFinder.FindAllWithInterface<IModuleSetupTasks>();
            modules.Clear();
            foreach (var module in modulesTypes)
            {
                var defaultConstructor = module.GetConstructor(Type.EmptyTypes);
                if (defaultConstructor == null)
                {
                    Debug.LogError($"FoundrySetupWizard was unable to load {module.Name} since it does not have a default constructor!");
                    continue;
                }

                var instance = Activator.CreateInstance(module);
                modules.Add(instance as IModuleSetupTasks);
            }
            
            foreach(var module in modules)
            {
                var state = module.GetTaskState();
                if (state == IModuleSetupTasks.State.UncompletedRequiredTasks)
                {
                    OpenWindow();
                    return;
                }
            }
        }
        
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            AssemblyReloadEvents.afterAssemblyReload += CheckForTasks;
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
            
            var refreshButton = new Button();
            var refreshIcon = new Image();
            refreshIcon.image = EditorGUIUtility.IconContent("d_Refresh").image;
            
            refreshButton.Add(refreshIcon);
            refreshButton.style.alignSelf = Align.FlexEnd;
            refreshButton.clicked += () =>
            {
                CheckForTasks();
            };
            root.Add(refreshButton);

            bool pendingTasks = false;

            foreach (var module in modules)
            {
                if(module.GetTaskState() == IModuleSetupTasks.State.Completed)
                    continue;
                
                var moduleBox = new Box();
                EditorUIUtils.SetPadding(moduleBox, 5);
                moduleBox.style.marginBottom = 7;
                moduleBox.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
                EditorUIUtils.SetBorderRadius(moduleBox, 8);
                
                VisualElement modName = new Label(module.ModuleName());
                modName.style.fontSize = 20f;
                modName.style.unityFontStyleAndWeight = FontStyle.Bold;
                    
                moduleBox.Add(modName);

                var modSource = new Label(module.ModuleSource());
                modSource.style.fontSize = 12f;
                modSource.style.unityFontStyleAndWeight = FontStyle.Italic;
                modSource.style.marginBottom = 10;
                
                moduleBox.Add(modSource);
                root.Add(moduleBox);
                
                
                var tasks = module.GetTasks();
                foreach (var list in tasks)
                {
                    VisualElement listBox = new Box();
                    listBox.style.backgroundColor = Color.clear;
                    moduleBox.Add(listBox);
                    
                    // VisualElements objects can contain other VisualElement following a tree hierarchy.
                    VisualElement header = new Label(list.name);
                    header.style.fontSize = 18f;
                    header.style.unityFontStyleAndWeight = FontStyle.Bold;

                    header.style.marginBottom = 10;
                    
                    listBox.Add(header);
                    
                    if(list.description != null)
                        listBox.Add(list.description);

                    foreach (var task in list.tasks)
                    {
                        pendingTasks = true;

                        var depContainer = new Box();
                        EditorUIUtils.SetPadding(depContainer, 10);
                        depContainer.style.marginBottom = 7;
                        depContainer.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
                        EditorUIUtils.SetBorderRadius(depContainer, 8);

                        var depLabel = new Label(task.name);
                        depLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                        depLabel.style.alignSelf = Align.Center;
                        depLabel.style.fontSize = fontSize;
                        depContainer.Add(depLabel);
                        if (task.description != null)
                        {
                            task.description.style.fontSize = fontSize;
                            task.description.style.marginTop = 4;
                            task.description.style.unityTextAlign = TextAnchor.MiddleLeft;
                            task.description.style.whiteSpace =
                                WhiteSpace.Normal;
                            depContainer.Add(task.description);
                        }

                        if (task.action != null)
                        {
                            var actionButton = new Button();
                            actionButton.Add(new Label(task.action.name));
                            actionButton.style.marginTop = 4;
                            actionButton.style.fontSize = fontSize;

                            actionButton.clicked += ()=>
                            {
                                // Disable the action after it's clicked so we don't double activate async actions
                                actionButton.SetEnabled(false);
                                task.action.callback();
                            };
                            depContainer.Add(actionButton);
                        }

                        listBox.Add(depContainer);
                    }
                }
            }
            
            if (!pendingTasks)
            {
                VisualElement noItems = new Label("Nothing left to do, you're all set! Thanks for using Foundry!");
                root.Add(noItems);
            }
        }
    }
}

