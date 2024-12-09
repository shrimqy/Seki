using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace Sefirah.App.Services;

/// <summary>
/// Default implementation of certificate loader.
/// </summary>
public class CertificateLoader
{
    /// <inheritdoc/>
    public static async Task<X509Certificate2> LoadCertificateAsync(string resourcePath, string password)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using Stream stream = assembly.GetManifestResourceStream(resourcePath)
            ?? throw new InvalidOperationException($"Certificate resource not found: {resourcePath}");

        byte[] certBytes = new byte[stream.Length];
        await stream.ReadAsync(certBytes);
        return new X509Certificate2(certBytes, password);
    }
}
