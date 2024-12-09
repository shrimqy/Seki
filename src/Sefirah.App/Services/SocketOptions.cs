namespace Sefirah.App.Services;

/// <summary>
/// Configuration options for the socket service.
/// </summary>
public class SocketOptions
{
    /// <summary>
    /// Default port for the socket server.
    /// </summary>
    public int DefaultPort { get; set; } = 5149;

    /// <summary>
    /// Path to the SSL certificate resource.
    /// </summary>
    public string CertificateResourcePath { get; set; } = "Seki.App.server.pfx";

    /// <summary>
    /// Password for the SSL certificate.
    /// </summary>
    public string CertificatePassword { get; set; } = "1864thround";
}
