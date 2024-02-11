using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Foundry
{
    /// <summary>
    /// Provides extensions for helping with async operations.
    /// </summary>
    static public class AsyncExtensions
    {
        /// <summary>
        /// Converts a Unity <see cref="AsyncOperation"/> into an awaitable <see cref="Task"/>.
        /// </summary>
        /// <param name="operation">
        /// The operation to convert.
        /// </param>
        /// <returns>
        /// The converted <see cref="Task"/>.
        /// </returns>
        static public Task AsTask(this AsyncOperation operation)
        {
            // Validate
            if (operation == null) { throw new ArgumentNullException(nameof(operation)); }

            // If the operation is already done, just return
            if (operation.isDone) { return Task.CompletedTask; }

            // Create a task completion source to handle completion
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            // Listen for completed
            operation.completed += (op)=>
            {
                tcs.SetResult(true);
            };

            // Return the task
            return tcs.Task;
        }

        /// <summary>
        /// Converts a Unity <see cref="AsyncOperation"/> into an awaitable <see cref="Task"/> 
        /// with progress changed events.
        /// </summary>
        /// <param name="operation">
        /// The operation to convert.
        /// </param>
        /// <param name="progress">
        /// The progress reporter which will be updated with progress.
        /// </param>
        /// <param name="status">
        /// The status message to report during the operation.
        /// </param>
        /// <param name="reportInterval">
        /// The interval in milliseconds at which progress reports are generated. The default is 250ms.
        /// </param>
        /// <returns>
        /// The converted <see cref="Task"/>.
        /// </returns>
        static public async Task AsTaskWithProgress(this AsyncOperation operation, IProgress<ProgressReport> progress, string status, int reportInterval=250)
        {
            // Validate
            if (operation == null) { throw new ArgumentNullException(nameof(operation)); }
            if (progress == null) { throw new ArgumentNullException(nameof(progress)); }

            // If the operation is already done, just return
            if (operation.isDone) { return; }

            // Continue while running
            while (!operation.isDone) 
            {
                // Report progress
                progress.Report(new ProgressReport(status, operation.progress));
                Debug.Log($"{status} {operation.progress}");

                // Wait before reporting agian
                await Task.Delay(reportInterval);
            }
        }
    }
}