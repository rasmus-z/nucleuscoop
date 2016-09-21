﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nucleus.Gaming
{
    public interface IGameInfo
    {
        /// <summary>
        /// A reference to the types used to create the 
        /// form steps needed for the user to successfully
        /// start the game
        /// </summary>
        Type[] Steps { get; }

        /// <summary>
        /// The game's executable name in lower case
        /// </summary>
        string ExecutableName { get; }

        /// <summary>
        /// This string must be on the game's executable path for it to be considered
        /// this game
        /// </summary>
        string[] ExecutableContext { get; }

        /// <summary>
        /// The game's name
        /// </summary>
        string GameName { get; }

        /// <summary>
        /// The class to instantiate to handle the game's
        /// initialization
        /// </summary>
        Type HandlerType { get; }

        /// <summary>
        /// If the user can change this game's windows positions
        /// </summary>
        bool SupportsPositioning { get; }

        /// <summary>
        /// The maximum number of players this game can handle
        /// </summary>
        int MaxPlayers { get; }
        
        /// <summary>
        /// The maximum number of players this game can handle in 1 monitor
        /// </summary>
        int MaxPlayersOneMonitor { get; }

        /// <summary>
        /// If the game supports keyboard and mouse gameplay
        /// </summary>
        bool SupportsKeyboard { get; }

        /// <summary>
        /// Custom options that the user can modify
        /// </summary>
        GameOption[] Options { get; }

        /// <summary>
        /// An unique GUID specific to the game
        /// </summary>
        string GUID { get; }
    }
}