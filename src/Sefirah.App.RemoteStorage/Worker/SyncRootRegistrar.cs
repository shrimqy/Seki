using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sefirah.App.RemoteStorage.Abstractions;
using Sefirah.App.RemoteStorage.Commands;
using Sefirah.App.RemoteStorage.Configuration;
using Sefirah.App.RemoteStorage.Interop;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Provider;

namespace Sefirah.App.RemoteStorage.Worker;
public class SyncRootRegistrar(
    IOptions<ProviderOptions> providerOptions,
    ILogger<SyncRootRegistrar> logger
)
{
    public IReadOnlyList<SyncRootInfo> GetSyncRoots()
    {
        var roots = StorageProviderSyncRootManager.GetCurrentSyncRoots();
        return roots
            .Where((x) => x.Id.StartsWith(providerOptions.Value.ProviderId + "!"))
            .Select((x) => new SyncRootInfo
            {
                Id = x.Id,
                Name = x.DisplayNameResource,
                Directory = x.Path.Path,
            })
            .ToArray();
    }

    public bool IsRegistered(string id) =>
        StorageProviderSyncRootManager.GetCurrentSyncRoots().Any((x) => x.Id == id);

    public StorageProviderSyncRootInfo Register<T>(RegisterSyncRootCommand command, IStorageFolder directory, T context) where T : struct
    {
        // Stage 1: Setup
        //--------------------------------------------------------------------------------------------
        // The client folder (syncroot) must be indexed in order for states to properly display
        var clientDirectory = new DirectoryInfo(command.Directory);
        clientDirectory.Attributes &= ~System.IO.FileAttributes.NotContentIndexed;

        var id = $"{providerOptions.Value.ProviderId}!{WindowsIdentity.GetCurrent().User}!{command.AccountId}";
        if (IsRegistered(id))
        {
            logger.LogWarning("Unexpectedly already registered {syncRootId}", id);
            Unregister(command.AccountId);
        }
        var contextBytes = StructBytes.ToBytes(context);
        var info = new StorageProviderSyncRootInfo
        {
            Id = id,
            Path = directory,
            DisplayNameResource = command.Name,
            IconResource = @"%SystemRoot%\system32\charmap.exe,0",
            HydrationPolicy = StorageProviderHydrationPolicy.Full,
            HydrationPolicyModifier = StorageProviderHydrationPolicyModifier.AllowFullRestartHydration,
            PopulationPolicy = (StorageProviderPopulationPolicy)command.PopulationPolicy,
            // Enable these to maintain better sync state
            InSyncPolicy = StorageProviderInSyncPolicy.FileCreationTime | 
                          StorageProviderInSyncPolicy.DirectoryCreationTime |
                          StorageProviderInSyncPolicy.Default,
            ShowSiblingsAsGroup = false,
            // TODO: Get version from package (but also don't crash on debug)
            Version = "1.0.0",
            // HardlinkPolicy = StorageProviderHardlinkPolicy.None,
            // RecycleBinUri = new Uri(""),
            Context = CryptographicBuffer.CreateFromByteArray(contextBytes),
        };
        // rootInfo.StorageProviderItemPropertyDefinitions.Add()

        logger.LogDebug("Registering {syncRootId}", id);
        StorageProviderSyncRootManager.Register(info);

        return info;
    }

    public void Unregister(string id)
    {
        logger.LogDebug("Unregistering {syncRootId}", id);
        try
        {
            StorageProviderSyncRootManager.Unregister(id);
        }
        catch (COMException ex) when (ex.HResult == -2147023728)
        {
            logger.LogWarning(ex, "Sync root not found");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unregister sync root failed");
        }
    }
}
