using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    public delegate void PlayerRigEvent(IPlayerControlRig rig);

    /// <summary>
    /// Service to manage the lifecycle of the player rig.
    /// </summary>
    public interface IPlayerRigManager
    {
        /// <summary>
        /// Grab the current instantiated player rig.
        /// </summary>
        public IPlayerControlRig Rig { get; }

        /// <summary>
        /// Return the current instantiated player rig.
        /// </summary>
        public void RegisterRig(IPlayerControlRig rig, Transform unusedRigHolder);

        /// <summary>
        /// Register the rig as in use
        /// </summary>
        public IPlayerControlRig BorrowPlayerRig();
    
        /// <summary>
        /// Register the rig as no longer in use
        /// </summary>
        public void ReturnPlayerRig();
    
        /// <summary>
        /// Event that fires when the player rig is first created.
        /// </summary>
        public event PlayerRigEvent PlayerRigCreated;

        public event PlayerRigEvent PlayerRigBorrowed;
        public event PlayerRigEvent PlayerRigReturned;
    }
}
