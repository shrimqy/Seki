using Microsoft.Extensions.Logging;
using Sefirah.App.RemoteStorage.Helpers;
using Sefirah.App.RemoteStorage.RemoteAbstractions;
using System.Threading.Channels;

namespace Sefirah.App.RemoteStorage.Worker.IO;
public sealed class RemoteWatcher(
    IRemoteReadService remoteReadService,
    IRemoteWatcher remoteWatcher,
    ChannelWriter<Func<Task>> taskWriter,
    FileLocker fileLocker,
    PlaceholdersService placeholderService,
    ILogger<RemoteWatcher> logger
) : IDisposable
{
    public void Start(CancellationToken stoppingToken)
    {
        remoteWatcher.Created += HandleCreated;
        remoteWatcher.Changed += HandleChanged;
        remoteWatcher.Renamed += HandleRenamed;
        remoteWatcher.Deleted += HandleDeleted;
        remoteWatcher.Start(stoppingToken);
    }

    private async Task HandleCreated(string relativePath)
    {
        logger.LogDebug("Created {path}", relativePath);
        await taskWriter.WriteAsync(async () =>
        {
            if (FileHelper.IsSystemFile(relativePath))
            {
                return;
            }
            using var locker = await fileLocker.Lock(relativePath);
            try
            {
                if (remoteReadService.IsDirectory(relativePath))
                {
                    await placeholderService.CreateOrUpdateDirectory(relativePath);
                }
                else
                {
                    await placeholderService.CreateOrUpdateFile(relativePath);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Handle Created failed");
            }
        });
    }

    private async Task HandleChanged(string relativePath)
    {
        logger.LogDebug("Changed {path}", relativePath);
        await taskWriter.WriteAsync(async () =>
        {
            using var locker = await fileLocker.Lock(relativePath);
            try
            {
                if (remoteReadService.IsDirectory(relativePath))
                {
                    await placeholderService.UpdateDirectory(relativePath);
                }
                else
                {
                    await placeholderService.UpdateFile(relativePath);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Handle Changed failed");
            }
        });
    }

    private async Task HandleRenamed(string oldRelativePath, string newRelativePath)
    {
        // Brief pause to let client rename finish before reflecting it back
        // await Task.Delay(1000);
        logger.LogDebug("Changed {oldPath} -> {path}", oldRelativePath, newRelativePath);
        await taskWriter.WriteAsync(async () =>
        {
            using var oldLocker = await fileLocker.Lock(oldRelativePath);
            using var newLocker = await fileLocker.Lock(newRelativePath);
            try
            {
                if (remoteReadService.IsDirectory(newRelativePath))
                {
                    await placeholderService.RenameDirectory(oldRelativePath, newRelativePath);
                }
                else
                {
                    await placeholderService.RenameFile(oldRelativePath, newRelativePath);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Rename placeholder failed");
            }
        });
    }

    private async Task HandleDeleted(string relativePath)
    {
        // Brief pause to let client rename finish before reflecting it back
        // await Task.Delay(1000);
        logger.LogDebug("Deleted {path}", relativePath);
        await taskWriter.WriteAsync(async () =>
        {
            using var locker = await fileLocker.Lock(relativePath);
            try
            {
                placeholderService.Delete(relativePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Delete placeholder failed");
            }
        });
    }

    public void Dispose()
    {
        remoteWatcher.Created -= HandleCreated;
        remoteWatcher.Changed -= HandleChanged;
        remoteWatcher.Renamed -= HandleRenamed;
        remoteWatcher.Deleted -= HandleDeleted;
    }
}
