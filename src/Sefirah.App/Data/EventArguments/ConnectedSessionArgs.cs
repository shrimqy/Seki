using Sefirah.App.Data.Models;

namespace Sefirah.App.Data.EventArguments;
public class ConnectedSessionArgs : EventArgs
{
    public bool IsConnected { get; set; }

    public string? SessionId { get; set; }

    public Device? Device { get; set; }
}
