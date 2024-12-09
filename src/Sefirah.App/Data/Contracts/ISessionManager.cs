﻿using Sefirah.App.Data.EventArguments;

namespace Sefirah.App.Data.Contracts;
public interface ISessionManager
{
    /// <summary>
    /// Event raised when client connection status changes, providing session connection details via ConnectedSessionArgs.
    /// </summary>
    event EventHandler<ConnectedSessionArgs> ClientConnectionStatusChanged;

    /// <summary>
    /// Sends a message to the connected client.
    /// </summary>
    /// <param name="message">The message to send.</param>
    void SendMessage(string message);

    /// <summary>
    /// Disconnects the current session.
    /// </summary>
    void DisconnectSession();
}
