using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Foundry.Services
{
    /// <summary>
    /// Extension methods to help with scene navigation.
    /// </summary>
    public static class SceneNavigationExtensions
    {
        #region Public Methods

        /// <summary>
        /// Converts a Unity Scene to a <see cref="SceneNavigationEntry" />.
        /// </summary>
        /// <param name="scene">
        /// The Unity scene to convert.
        /// </param>
        /// <returns>
        /// The navigation entry.
        /// </returns>
        public static SceneNavigationEntry AsNavigationEntry(this UnityEngine.SceneManagement.Scene scene)
        {
            return new SceneNavigationEntry()
            {
                Name = scene.name,
                BuildIndex = scene.buildIndex,
            };
        }

        #endregion Public Methods
    }

    /// <summary>
    /// Represents an entry in navigation history.
    /// </summary>
    public class SceneNavigationEntry : ISceneNavigationEntry
    {
        #region Public Properties

        /// <inheritdoc />
        public int BuildIndex { get; set; }

        /// <inheritdoc />
        public string Name { get; set; }

        #endregion Public Properties
    }

    /// <summary>
    /// Implementation of the <see cref="ISceneNavigator" /> service.
    /// </summary>
    public class SceneNavigator : ISceneNavigator
    {
        #region Member Variables

        private int currentIndex = -1;
        private List<SceneNavigationEntry> history = new();
        private Task navigationTask;
        private Progress<ProgressReport> progress = new Progress<ProgressReport>();
        private Dictionary<string, int> sceneNameToBuildIndexMap = new Dictionary<string, int>();

        #endregion Member Variables

        /// <summary>
        /// Initialzies a new <see cref="SceneNavigator"/> instance.
        /// </summary>
        public SceneNavigator() 
        {
            StoreInitialScene();
            
            for(int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                var sceneName = scenePath.Substring(scenePath.LastIndexOf('/') + 1).Replace(".unity", "");
                if(!sceneNameToBuildIndexMap.TryAdd(sceneName, i))
                    Debug.LogWarning($"Two scenes with duplicate names {sceneName} found in build settings! This will cause issues with navigation if you're using scene names instead of paths or build indices!");
            }
        }

        #region Private Methods

        /// <summary>
        /// Loads the specified Unity scene and adds it to history.
        /// </summary>
        /// <param name="entry">
        /// The navigation entry to load.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> that represents the operation.
        /// </returns>
        private async Task LoadEntryAndAddHistoryAsync(SceneNavigationEntry entry)
        {
            // If we're currently in the history back stack, truncate history to the current item
            if (currentIndex < (history.Count - 1))
            {
                int firstToRemove = currentIndex + 1;
                history.RemoveRange(firstToRemove, history.Count - (firstToRemove));
            }

            // Add it to the history, use entry from scene manager to fill empty fields
            if(string.IsNullOrEmpty(entry.Name))
                entry.Name = SceneUtility.GetScenePathByBuildIndex(entry.BuildIndex);
            else if(sceneNameToBuildIndexMap.TryGetValue(entry.Name, out int buildIndex))
                entry.BuildIndex = buildIndex;

            history.Add(entry);
            // Update the index to be at the end of the collection
            currentIndex = history.Count - 1;

            //Load after we have added scene to history 
            await LoadEntryAsync(entry);
        }

        /// <summary>
        /// Loads the specified entry.
        /// </summary>
        /// <param name="newEntry">
        /// The entry to load.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> that represents the operation.
        /// </returns>
        /// <remarks>
        /// This is the method that all other methods call and actually performs the scene load.
        /// </remarks>
        private async Task LoadEntryAsync(SceneNavigationEntry newEntry)
        {
            // Validate
            if (newEntry == null) { throw new ArgumentNullException(nameof(newEntry)); }

            // Placeholder for scene display name
            string displayName = null;

            // Placeholder for load operation
            AsyncOperation loadOp = null;

            // Start load
            if (!string.IsNullOrEmpty(newEntry.Name))
            {
                // Display name is the scene name
                displayName = newEntry.Name;

                // Load the scene
                loadOp = SceneManager.LoadSceneAsync(newEntry.Name);
            }
            else
            {
                // Display name is based on index
                displayName = $"Scene{newEntry.BuildIndex:00}";

                // Load the scene
                loadOp = SceneManager.LoadSceneAsync(newEntry.BuildIndex);
            }

            // Make sure we have an operation
            if (loadOp == null)
            {
                throw new InvalidOperationException("Could not load specified scene.");
            }

            // Notify subscribers of starting
            NavigationStarting?.Invoke(newEntry);

            // Delay a few milliseconds for the loading visuals to appear before bogging things down with loading
            await Task.Delay(100);

            // Convert the load operation to a task with status updates
            // Because we're using the progress reporter, these will bubble up
            // to our ProgressChanged event on this class and they will be
            // emitted on the UI thread.
            navigationTask = loadOp.AsTaskWithProgress(progress, $"Loading '{displayName}'...");

            // Notify subscribers of started
            NavigationStarted?.Invoke(newEntry);

            // Wait on the load task to complete
            await navigationTask;

            // Notify subscribers of complete
            NavigationCompleted?.Invoke(newEntry);
        }

        /// <summary>
        /// Loads the entry at the specified index in navigation history.
        /// </summary>
        /// <param name="targetIndex">
        /// The target index of the history entry.
        /// </param>
        /// <param name="updateCurrentIndex">
        /// Whether the current index should be updated to the target index.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> that represents the operation.
        /// </returns>
        private async Task LoadHistoryIndexAsync(int targetIndex, bool updateCurrentIndex = true)
        {
            // Get the matching entry
            SceneNavigationEntry entry = history[targetIndex];
            
            // Update index before switching so init scripts work correctly
            if (updateCurrentIndex)
            {
                // Update the current index
                currentIndex = targetIndex;
            }

            // Navigate to the previous entry
            await LoadEntryAsync(entry);
            
        }

        /// <summary>
        /// Saves the current scene as the initial scene in the history.
        /// </summary>
        private void StoreInitialScene()
        {
            // Get the current scene
            var scene = SceneManager.GetActiveScene();

            // Create a history entry for it
            history.Insert(0, scene.AsNavigationEntry());

            // Set the current index
            currentIndex = 0;
        }

        #endregion Private Methods

        #region Public Methods

        /// <inheritdoc />
        public Task GoBackAsync()
        {
            // Make sure navigation is valid
            if (!CanGoBack) { throw new InvalidOperationException("No entry to go back to."); }

            // Go to index
            return LoadHistoryIndexAsync(currentIndex - 1);
        }

        /// <inheritdoc />
        public Task GoForwardAsync()
        {
            // Make sure navigation is valid
            if (!CanGoForward) { throw new InvalidOperationException("No entry to go forward to."); }

            // Go to index
            return LoadHistoryIndexAsync(currentIndex + 1);
        }

        /// <inheritdoc />
        public Task GoToAsync(string sceneName)
        {
            // Load it and add to history
            return LoadEntryAndAddHistoryAsync(new SceneNavigationEntry()
            {
                Name = sceneName,
                BuildIndex = -1
            });
        }

        /// <inheritdoc />
        public Task GoToAsync(int sceneBuildIndex)
        {
            // Load it and add to history
            return LoadEntryAndAddHistoryAsync(new SceneNavigationEntry()
            {
                Name = null,
                BuildIndex = sceneBuildIndex
            });
        }

        #endregion Public Methods

        #region Public Properties

        /// <inheritdoc />
        public bool CanGoBack
        {
            get
            {
                return currentIndex > 0;
            }
}

        /// <inheritdoc />
        public bool CanGoForward
        {
            get
            {
                return currentIndex < (history.Count - 1);
            }
        }

        /// <inheritdoc />
        public ISceneNavigationEntry CurrentScene
        {
            get
            {
                return (currentIndex > -1 ? history[currentIndex] : null);
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<ISceneNavigationEntry> History => history;

        /// <inheritdoc />
        public bool IsNavigating => (NavigationTask != null && !NavigationTask.IsCompleted);

        /// <inheritdoc />
        public Task NavigationTask => navigationTask;

        #endregion Public Properties

        #region Public Events

        /// <inheritdoc />
        public event NavigationEvent NavigationCompleted;

        /// <inheritdoc />
        public event NavigationEvent NavigationStarted;

        /// <inheritdoc />
        public event NavigationEvent NavigationStarting;

        /// <inheritdoc />
        public event EventHandler<ProgressReport> ProgressChanged
        {
            add => progress.ProgressChanged += value;
            remove => progress.ProgressChanged -= value;
        }

        #endregion Public Events

    }
}