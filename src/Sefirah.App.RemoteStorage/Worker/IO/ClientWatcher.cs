using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using Sefirah.App.RemoteStorage.RemoteAbstractions;
using Sefirah.App.RemoteStorage.Abstractions;
using Sefirah.App.RemoteStorage.Helpers;
using Sefirah.App.RemoteStorage.Interop;

namespace Sefirah.App.RemoteStorage.Worker.IO;
public class ClientWatcher : IDisposable
{
    private readonly ISyncProviderContextAccessor _contextAccessor;
    private readonly ChannelWriter<Func<Task>> _taskWriter;
    private readonly FileLocker _fileLocker;
    private readonly IRemoteReadWriteService _remoteService;
    private readonly PlaceholdersService _placeholdersService;
    private readonly ILogger _logger;
    private readonly FileSystemWatcher _watcher;

    private string _rootDirectory => _contextAccessor.Context.RootDirectory;

    public ClientWatcher(
        ISyncProviderContextAccessor contextAccessor,
        ChannelWriter<Func<Task>> taskWriter,
        FileLocker fileLocker,
        IRemoteReadWriteService remoteService,
        PlaceholdersService placeholdersService,
        ILogger<ClientWatcher> logger
    )
    {
        _contextAccessor = contextAccessor;
        _taskWriter = taskWriter;
        _fileLocker = fileLocker;
        _remoteService = remoteService;
        _placeholdersService = placeholdersService;
        _logger = logger;
        _watcher = CreateWatcher();
    }

    private FileSystemWatcher CreateWatcher()
    {
        var watcher = new FileSystemWatcher(_rootDirectory)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName
                | NotifyFilters.DirectoryName
                | NotifyFilters.Attributes
                | NotifyFilters.LastWrite,
            InternalBufferSize = 64 * 1024,
        };

        watcher.Changed += async (object sender, FileSystemEventArgs e) => {
            if (e.ChangeType != WatcherChangeTypes.Changed || !Path.Exists(e.FullPath) || FileHelper.IsSystemFile(e.FullPath))
            {
                return;
            }
            var fileInfo = new FileInfo(e.FullPath);
            _logger.LogDebug("{changeType} {path}", e.ChangeType, e.FullPath);
            await _taskWriter.WriteAsync(async () => {
                var relativePath = PathMapper.GetRelativePath(e.FullPath, _rootDirectory);
                try
                {
                    if (fileInfo.Attributes.HasAllSyncFlags(SyncAttributes.PINNED | (int)FileAttributes.Offline))
                    {
                        if (fileInfo.Attributes.HasFlag(FileAttributes.Directory))
                        {
                            _placeholdersService.CreateBulk(relativePath);
                            var childItems = Directory.EnumerateFiles(e.FullPath, "*", SearchOption.AllDirectories)
                                .Where((x) => !FileHelper.IsSystemFile(x))
                                .ToArray();
                            foreach (var childItem in childItems)
                            {
                                try
                                {
                                    CloudFilter.HydratePlaceholder(childItem);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Hydate file failed: {filePath}", childItem);
                                }
                            }
                        }
                        else
                        {
                            CloudFilter.HydratePlaceholder(e.FullPath);
                        }
                    }
                    else if (
                        fileInfo.Attributes.HasAnySyncFlag(SyncAttributes.UNPINNED)
                        && !fileInfo.Attributes.HasFlag(FileAttributes.Offline)
                        && !fileInfo.Attributes.HasFlag(FileAttributes.Directory)
                    )
                    {
                        CloudFilter.DehydratePlaceholder(e.FullPath, relativePath, fileInfo.Length);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Hydrate/dehydrate failed");
                }

                using var locker = await _fileLocker.Lock(relativePath);
                if (fileInfo.Attributes.HasFlag(FileAttributes.Directory))
                {
                    //var directoryInfo = new DirectoryInfo(e.FullPath);
                    //await _serverService.UpdateDirectory(directoryInfo, relativePath);
                }
                else
                {
                    await _remoteService.UpdateFile(fileInfo, relativePath);
                }
            });
        };

        watcher.Created += async (object sender, FileSystemEventArgs e) => {
            if (FileHelper.IsSystemFile(e.FullPath))
            {
                return;
            }
            _logger.LogDebug("Created {path}", e.FullPath);
            await _taskWriter.WriteAsync(async () => {
                var relativePath = PathMapper.GetRelativePath(e.FullPath, _rootDirectory);

                using var locker = await _fileLocker.Lock(relativePath);
                if (File.GetAttributes(e.FullPath).HasFlag(FileAttributes.Directory))
                {
                    var directoryInfo = new DirectoryInfo(e.FullPath);
                    await _remoteService.CreateDirectory(directoryInfo, relativePath);
                    var childItems = Directory.EnumerateFiles(e.FullPath, "*", SearchOption.AllDirectories)
                        .Where((x) => !FileHelper.IsSystemFile(x))
                        .ToArray();
                    foreach (var childItem in childItems)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(childItem);
                            await _remoteService.CreateFile(fileInfo, childItem);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Create file failed: {filePath}", childItem);
                        }
                    }
                }
                else
                {
                    var fileInfo = new FileInfo(e.FullPath);
                    await _remoteService.CreateFile(fileInfo, relativePath);
                }
            });
        };

        watcher.Error += (object sender, ErrorEventArgs e) => {
            var ex = e.GetException();
            _logger.LogError(ex, "Client file watcher error");
        };

        return watcher;
    }

    public void Start()
    {
        _watcher.EnableRaisingEvents = true;
    }

    public void Dispose()
    {
        _watcher.Dispose();
    }
}