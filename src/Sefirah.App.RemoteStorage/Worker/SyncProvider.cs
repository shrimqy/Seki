using Microsoft.Extensions.Logging;
using Sefirah.App.RemoteStorage.Abstractions;
using Sefirah.App.RemoteStorage.Async;
using Sefirah.App.RemoteStorage.Helpers;
using Sefirah.App.RemoteStorage.Worker.IO;
using static Vanara.PInvoke.CldApi;

namespace Sefirah.App.RemoteStorage.Worker;
public class SyncProvider(
    ISyncProviderContextAccessor contextAccessor,
    TaskQueue taskQueue,
    ShellCommandQueue shellCommandQueue,
    SyncRootConnector syncProvider,
    PlaceholdersService placeholdersService,
    ClientWatcher clientWatcher,
    RemoteWatcher remoteWatcher,
    ILogger<SyncProvider> logger
)
{
    public async Task Run(CancellationToken cancellation)
    {
        taskQueue.Start(cancellation);
        shellCommandQueue.Start(cancellation);

        logger.LogDebug("Connecting...");
        // Hook up callback methods (in this class) for transferring files between client and server
        using var connectDisposable = new Disposable<CF_CONNECTION_KEY>(syncProvider.Connect(), syncProvider.Disconnect);

        // Create the placeholders in the client folder so the user sees something
        if (contextAccessor.Context.PopulationPolicy == Commands.PopulationPolicy.AlwaysFull)
        {
            placeholdersService.CreateBulk(string.Empty);
        }

        // TODO: Sync changes since last time this service ran

        // Stage 2: Running
        //--------------------------------------------------------------------------------------------
        // The file watcher loop for this sample will run until the user presses Ctrl-C.
        // The file watcher will look for any changes on the files in the client (syncroot) in order
        // to let the cloud know.
        clientWatcher.Start();
        remoteWatcher.Start(cancellation);

        // Run until SIGTERM
        await cancellation;

        logger.LogDebug("Stopping shell command queue...");
        await shellCommandQueue.Stop();
        logger.LogDebug("Stopping task queue...");
        await taskQueue.Stop();

        logger.LogDebug("Disconnecting...");
        // TODO: Only on uninstall (or not at all?)
        //placeholdersService.DeleteBulk(directory);
    }
}
