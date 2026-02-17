using System;

namespace Foundry
{
    /// <summary>
    /// A class that provides updates on the progress of a long-running task.
    /// </summary>
    public class ProgressReport
    {
        #region Public Constructors

        /// <summary>
        /// Initializes a new <see cref="ProgressReport" />.
        /// </summary>
        /// <param name="status">
        /// A message that describes the current state of the task.
        /// </param>
        /// <param name="progress">
        /// A floating point value between 0.0 and 1.0 that represents the percentage of completion.
        /// </param>
        public ProgressReport(string status, float progress)
        {
            Status = status;
            Progress = progress;
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// A floating point value between 0.0 and 1.0 that represents the percentage of completion.
        /// </summary>
        public float Progress { get; private set; }

        /// <summary>
        /// A message that describes the current state of the task.
        /// </summary>
        public string Status { get; private set; }

        #endregion Public Properties
    }
}