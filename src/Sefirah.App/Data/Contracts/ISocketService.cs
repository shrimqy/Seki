namespace Sefirah.App.Data.Contracts;

public interface ISocketService
{
    /// <summary>
    /// Gets the current port number the server is running on.
    /// </summary>
    int Port { get; }

    /// <summary>
    /// Gets whether the server is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Starts the socket server asynchronously.
    /// </summary>
    /// <param name="port">Optional port number. If not specified, uses the default port.</param>
    /// <returns>True if server started successfully, false otherwise.</returns>
    Task<bool> StartServerAsync(int? port = null);

    /// <summary>
    /// Stops the socket server.
    /// </summary>
    void StopServer();
}