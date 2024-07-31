using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;

namespace Foundry.Services
{
    /// <summary>
    /// Event delegate for navigation events
    /// </summary>
    public delegate void NavigationEvent(ISceneNavigationEntry scene);
    
    /// <summary>
    /// A service that allows navigation between scenes.
    /// </summary>
    public interface ISceneNavigator : IProgressChangedEvent
    {
        #region Public Methods

        /// <summary>
        /// Navigates to the last scene in the navigation history.
        /// </summary>
        /// <remarks>
        /// If there is no entry in the history, calling this method will result in an <see
        /// cref="InvalidOperationException" />.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// There is no history entry to go back to.
        /// </exception>
        /// <returns>
        /// A <see cref="Task" /> that represents the operation.
        /// </returns>
        Task GoBackAsync();

        /// <summary>
        /// Navigates to the last scene in the navigation history.
        /// </summary>
        /// <remarks>
        /// If there is no entry in the history, calling this method will result in an <see
        /// cref="InvalidOperationException" />.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// There is no history entry to go back to.
        /// </exception>
        /// <returns>
        /// A <see cref="Task" /> that represents the operation.
        /// </returns>
        Task GoForwardAsync();

        /// <summary>
        /// Navigates to the specified scene in the background.
        /// </summary>
        /// <param name="sceneName">
        /// The name of the scene to navigate to.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> that represents the operation.
        /// </returns>
        Task GoToAsync(string sceneName);

        /// <summary>
        /// Navigates to the specified scene in the background.
        /// </summary>
        /// <param name="sceneBuildIndex">
        /// The index of the scene to navigate to.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> that represents the operation.
        /// </returns>
        Task GoToAsync(int sceneBuildIndex);


        /// <summary>
        /// Navigates to the specified scene addressable in the background.
        /// </summary>
        /// <param name="sceneAsset">Addressable that must me a scene</param>
        /// <returns></returns>
        public Task GoToAsync(AssetReference sceneAsset);

        #endregion Public Methods

        #region Public Properties

        /// <summary>
        /// Gets a value that indicates if there is a valid item in the history to navigate back to.
        /// </summary>
        /// <remarks>
        /// <c>true</c> if there is a valid item in the history to navigate back to; otherwise <c>false</c>.
        /// </remarks>
        bool CanGoBack { get; }

        /// <summary>
        /// Gets a value that indicates if there is a valid item in the history to navigate forward to.
        /// </summary>
        /// <remarks>
        /// <c>true</c> if there is a valid item in the history to navigate forward to; otherwise <c>false</c>.
        /// </remarks>
        bool CanGoForward { get; }

        /// <summary>
        /// Gets the current scene.
        /// </summary>
        ISceneNavigationEntry CurrentScene { get; }

        /// <summary>
        /// Gets the navigation history.
        /// </summary>
        IReadOnlyList<ISceneNavigationEntry> History { get; }

        /// <summary>
        /// Gets a value that indicates if a navigation is in progress.
        /// </summary>
        bool IsNavigating { get; }

        /// <summary>
        /// Gets a task that represents the most current navigation operation.
        /// </summary>
        Task NavigationTask { get; }

        #endregion Public Properties

        #region Public Events

        /// <summary>
        /// Raised whenever a navigation operation has completed.
        /// </summary>
        event NavigationEvent NavigationCompleted;

        /// <summary>
        /// Raised whenever a navigation operation has started.
        /// </summary>
        /// <remarks>
        /// This happens when the actual load operation begins. 
        /// <see cref="NavigationTask"/> will be valid during this event.
        /// </remarks>
        event NavigationEvent NavigationStarted;

        /// <summary>
        /// Raised whenever a navigation operation is starting.
        /// </summary>
        /// <remarks>
        /// This happens a few milliseconds before the actual load operation begins. 
        /// <see cref="NavigationTask"/> will not be valid during this event.
        /// </remarks>
        event NavigationEvent NavigationStarting;

        #endregion // Public Events
    }
}