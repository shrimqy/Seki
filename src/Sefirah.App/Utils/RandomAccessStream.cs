using System.IO;
using Windows.Storage.Streams;

namespace Sefirah.App.Utils;
public class RandomAccessStream
{

    public static async Task<IRandomAccessStream> ConvertToRandomAccessStreamAsync(Stream stream)
    {
        var randomAccessStream = new InMemoryRandomAccessStream();
        var outputStream = randomAccessStream.GetOutputStreamAt(0);

        //using (var inputStream = stream.AsInputStream())
        //{
        //    IRandomAccessStream randomAccessStream = stream.AsRandomAccessStream();
        //}

        await outputStream.FlushAsync();
        return randomAccessStream;
    }
}
