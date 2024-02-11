using System;

namespace Foundry.Core.Editor
{
    public interface IServiceDefinition
    {
        /// <summary>
        /// The package that defines this system
        /// </summary>
        /// <returns>A package name (com.example.name)</returns>
        string Source();

        /// <summary>
        /// Name of the system, used for display purposes
        /// </summary>
        string PrettyName();

        /// <summary>
        /// The interface that this system should be implemented with
        /// </summary>
        /// <returns>typeof(interface)</returns>
        Type ServiceInterface();
    }
}
