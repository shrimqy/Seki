using Sefirah.App.Data.Contracts;
using Sefirah.App.Data.Models;
using Sefirah.App.RemoteStorage.Commands;
using Sefirah.App.RemoteStorage.RemoteSftp;
using Sefirah.App.RemoteStorage.Worker;
using Sefirah.App.Utils;
using Windows.Storage;

namespace Sefirah.App.Services;

public class SftpService(
    ILogger logger,
    SyncRootRegistrar registrar,
    SyncProviderPool syncProviderPool
    ) : ISftpService
{
    private readonly ILogger _logger = logger;
    private readonly SyncRootRegistrar _registrar = registrar;
    private readonly SyncProviderPool _syncProviderPool = syncProviderPool;

    public async Task InitializeAsync(SftpServerInfo info)
    {
        try
        {
            var sftpContext = new SftpContext
            {
                Host = info.IpAddress,
                Port = info.Port,
                Directory = "/",
                Username = info.Username,
                Password = info.Password,
                WatchPeriodSeconds = 2,
            };

            await Register(
                name: "Tester",
                directory: "C:\\Users\\shrim\\tes",
                accountId: "4213129849231255347",
                context: sftpContext
            );
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to initialize SFTP service", ex);
            throw;
        }
    }

    private async Task Register<T>(string name, string directory, string accountId, T context) where T : struct 
    {
        try 
        {
            var registerCommand = new RegisterSyncRootCommand
            {
                Name = name,
                Directory = directory,
                AccountId = accountId,
                PopulationPolicy = PopulationPolicy.Full,
            };
            var storageFolder = await StorageFolder.GetFolderFromPathAsync(registerCommand.Directory);
            var info = _registrar.Register(registerCommand, storageFolder, context);
            _syncProviderPool.Start(info);
        }
        catch (Exception ex) 
        {
            _logger.Error($"Failed to register sync root. Directory: {directory}, AccountId: {accountId}", ex);
            throw;
        }
    }
 
}
