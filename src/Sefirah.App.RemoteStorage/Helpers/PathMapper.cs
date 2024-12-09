namespace Sefirah.App.RemoteStorage.Helpers;
public static class PathMapper
{
    public static string GetRelativePath(string fullPath, string startPath) =>
        fullPath.Length == startPath.Length
            ? string.Empty
            : fullPath[(startPath.Length + 1)..];

    public static string ReplaceStart(string source, string oldStart, string newStart) =>
        string.Concat(newStart, source[oldStart.Length..]);

    public static void EnsureSubDirectoriesExist(string path)
    {
        var directory = Path.GetDirectoryName(path)!;
        if (Path.Exists(directory))
        {
            return;
        }
        Directory.CreateDirectory(directory);
    }
}
