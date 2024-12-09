using Sefirah.App.Data.Models;

namespace Sefirah.App.Data.Contracts;

public interface ISftpService
{
    Task InitializeAsync(SftpServerInfo info);
}
