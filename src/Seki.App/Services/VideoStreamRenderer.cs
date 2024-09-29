using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;

namespace Seki.App.Services
{
    public class VideoStreamHandler : IDisposable
    {
        private MediaSource _mediaSource;
        private IRandomAccessStream _stream;
        private DataWriter _dataWriter;

        public VideoStreamHandler()
        {
            _stream = new InMemoryRandomAccessStream();
            _dataWriter = new DataWriter(_stream.GetOutputStreamAt(0));
            _mediaSource = MediaSource.CreateFromStream(_stream, "video/mp4");
        }

        public MediaSource MediaSource => _mediaSource;

        public void ProcessFrame(byte[] frameData)
        {
            _dataWriter.WriteBytes(frameData);
            _dataWriter.StoreAsync().AsTask().Wait();
            _stream.Seek(0);
        }

        public void Dispose()
        {
            _mediaSource?.Dispose();
            _dataWriter?.Dispose();
            _stream?.Dispose();
        }
    }
}
