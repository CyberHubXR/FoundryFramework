using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry.Networking
{
    /// <summary>
    /// Represents the type of session that we wish to use
    /// </summary>
    [Flags]
    public enum SessionType
    {
        /// <summary>
        /// All clients own their own data
        /// </summary>
        Shared = 1,
        /// <summary>
        /// Client connects to a server or host 
        /// </summary>
        Client = 1 << 1, 
        /// <summary>
        ///  Server hosts a session, is not a player
        /// </summary>
        Server = 1 << 2, 
        /// <summary>
        ///  Host is a server and a player, and provides a session to clients
        /// </summary>
        Host = 1 << 3
    }
    
    
    /// <summary>
    /// Info required to start a session
    /// </summary>
    public interface ISessionInfo
    {
        /// <summary>
        /// The name of the session to join or start
        /// </summary>
        public string sessionName { get; set; }
        
        /// <summary>
        /// The type of session
        /// </summary>
        public SessionType sessionType { get; set; }
    }
    
    /// <summary>
    /// Base type for providing session info
    /// </summary>
    public class SessionInfo : ISessionInfo
    {
        /// <inheritdoc />
        public string sessionName { get; set; }
        
        /// <inheritdoc />
        public SessionType sessionType { get; set; }
    }
}
