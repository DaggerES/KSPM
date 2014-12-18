//using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

/// <summary>
/// Defines some properties that are required by the KSPMSystem inside Unity.
/// </summary>
public class KSPMUnitySystem : KSPM.Globals.KSPMSystem
{
    ///Defines those commands that KSPM uses to interact with KSP system.
    #region KSPMCommands

    /// <summary>
    /// Holds those commands that can be sent by sent by the system.
    /// </summary>
    public enum GameCommand : byte
    {
        /// <summary>
        /// Invalid command.
        /// </summary>
        Null = 254,

        /// <summary>
        /// The system has changed from one escene to another.
        /// </summary>
        SceneChanged = 1,

        ShipSync,

        /// <summary>
        /// Asks for an unique id inside the system.
        /// </summary>
        RequestVesselId,
    }

    public enum FlightCommand : byte
    {
        Force,
    }

    #endregion

    /// <summary>
    /// Defines those available states of the KSPM objects.
    /// </summary>
    public enum KSPMStates : byte
    {
        /// <summary>
        /// Invalid stated, something happended.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// Means that the KSPM module is started but not connected.
        /// </summary>
        NotConnected,

        /// <summary>
        /// KSPM module is started and connected.
        /// </summary>
        ReadyToGo,


    }

    /// <summary>
    /// Defines the state machine that the multiplayer can have.
    /// </summary>
    public enum KSPMGameStates : byte
    {
        Undefined = 0,

        ChangingScenes
    }
}
