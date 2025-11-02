namespace Broker.Core.Storage.Models;

/// <summary>
/// Represents the state of a QoS 2 message in the two-phase handshake.
/// </summary>
public enum Qos2State
{
    /// <summary>
    /// Waiting for PUBREC (broker sent PUBLISH, waiting for client PUBREC).
    /// </summary>
    WaitingForPubRec,

    /// <summary>
    /// Waiting for PUBCOMP (broker sent PUBREL, waiting for client PUBCOMP).
    /// </summary>
    WaitingForPubComp,

    /// <summary>
    /// Completed (PUBCOMP received, message can be removed).
    /// </summary>
    Completed
}

