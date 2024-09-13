using Seki.App.Data.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Seki.App.Services
{
    public class CommandService
    {
        private static CommandService? _instance;
        public static CommandService Instance => _instance ??= new CommandService();


        private HttpListener _listener;

        public async Task HandleCommandMessageAsync(Command message)
        {

            if (!Enum.TryParse(message.CommandType, true, out CommandType command))
            {
                System.Diagnostics.Debug.WriteLine($"Unknown action: {message.Type}");
                return;
            }
        }

        private void StartHttpServer()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://+:8080/upload/");
            _listener.Start();
            _listener.BeginGetContext(new AsyncCallback(HandleRequest), _listener);
        }

        private async void HandleRequest(IAsyncResult result)
        {
            HttpListenerContext context = _listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;

            // Handle the incoming request
            if (request.HttpMethod == "POST")
            {
                using (Stream body = request.InputStream)
                {
                    var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ReceivedFile");
                    using (var fileStream = File.Create(filePath))
                    {
                        await body.CopyToAsync(fileStream);
                    }
                }
            }

            // Respond to the request
            HttpListenerResponse response = context.Response;
            string responseString = "<html><body>File received</body></html>";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            using (Stream output = response.OutputStream)
            {
                await output.WriteAsync(buffer, 0, buffer.Length);
            }

            _listener.BeginGetContext(new AsyncCallback(HandleRequest), _listener);
        }

        private void StopHttpServer()
        {
            if (_listener != null)
            {
                _listener.Stop();
                _listener.Close();
            }
        }
    }
} 

