using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry.Core.Setup
{
    /// <summary>
    /// A module can provide an implementation of this class to register setup tasks. It will automatically be detected and added to the FoundrySetupWizard.
    /// </summary>
    public interface IModuleSetupTasks
    {
        enum State
        {
            UncompletedRequiredTasks,
            UncompletedOptionalTasks,
            Completed
        }
        
        /// <returns>Current state of this module's setup tasks</returns>
        /// <remarks>
        /// This is used by the FoundrySetupWizard to decide whether to pop-up and prompt the user to take action or not.
        /// </remarks>
        State GetTaskState();
        
        /// <returns>Uncompleted tasks that should be displayed in the FoundrySetupWizard, grouped by lists</returns>
        List<SetupTaskList> GetTasks();

        /// <returns>The name of the module these tasks are for.</returns>
        string ModuleName();

        /// <returns>The location of this module (com.example.package)</returns>
        string ModuleSource();
    }
}
