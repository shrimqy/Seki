namespace Sefirah.App.Data.Contracts;
public interface IUpdateService : INotifyPropertyChanged
{
    /// <summary>
    /// Gets a value indicating whether updates are available.
    /// </summary>
    bool IsUpdateAvailable { get; }

    /// <summary>
    /// Gets a value indicating if release notes are available.
    /// </summary>
    bool IsReleaseNotesAvailable { get; }

    Task CheckForUpdatesAsync();

    Task CheckLatestReleaseNotesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets release notes for the latest release
    /// </summary>
    Task<string?> GetLatestReleaseNotesAsync(CancellationToken cancellationToken = default);
}