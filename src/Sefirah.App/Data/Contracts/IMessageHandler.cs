using Sefirah.App.Data.Models;
using Sefirah.App.Services;

namespace Sefirah.App.Data.Contracts;

public interface IMessageHandler
{
    /// <summary>
    /// Handles a JSON message received from a client.
    /// </summary>
    /// <param name="message">The JSON message received.</param>
    /// <param name="session">The session associated with the message.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    void HandleJsonMessage(SocketMessage message, SekiSession session);

    /// <summary>
    /// TODO: Implement this method.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    Task HandleBinaryData(byte[] data);
}
