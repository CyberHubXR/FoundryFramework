using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Foundry
{
    /// <summary>
    /// Interface for a class that provides a ProgressChanged event with a custom report type.
    /// </summary>
    /// <typeparam name="TProgress">
    /// The type of data reported by the ProgressChanged event.
    /// </typeparam>
    public interface IProgressChangedEvent<TProgress>
    {
        /// <summary>
        /// Raised for each reported progress value.
        /// </summary>
        event EventHandler<TProgress> ProgressChanged;
    }

    /// <summary>
    /// Interface for a class that provides a ProgressChanged event with the report type of 
    /// <see cref="ProgressReport"/>.
    /// </summary>
    public interface IProgressChangedEvent : IProgressChangedEvent<ProgressReport>
    {
    }
}
