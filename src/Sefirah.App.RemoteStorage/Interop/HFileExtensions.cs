using Sefirah.App.RemoteStorage.Interop.Extensions;
using Vanara.PInvoke;

namespace Sefirah.App.RemoteStorage.Interop;
public static class HFileExtensions
{
    public static SafeMetaHFILE ToMeta(this Kernel32.SafeHFILE fileHandle) => new SafeMetaHFILE.Kernel32HFILE(fileHandle);
    public static SafeMetaHFILE ToMeta(this SafeOplockHFILE fileHandle) => new SafeMetaHFILE.OplockHFILE(fileHandle);
    public static SafeMetaHFILE ToMeta(this CldApi.SafeHCFFILE fileHandle) => new SafeOplockHFILE(fileHandle).ToMeta();
    public static SafeMetaHFILE ThrowIfInvalid(this SafeMetaHFILE fileHandle, string path)
    {
        if (!((HFILE)fileHandle).IsInvalid)
        {
            return fileHandle;
        }
        fileHandle.Dispose();
        throw new HFileException(path);
    }
}
