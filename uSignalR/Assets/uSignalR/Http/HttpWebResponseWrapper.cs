using System;
using System.IO;
using System.Net;
using uTasks;

namespace uSignalR.Http
{
    public class HttpWebResponseWrapper : IResponse
    {
        private readonly HttpWebResponse _response;

        public HttpWebResponseWrapper(HttpWebResponse response)
        {
            _response = response;
        }

        public Task<string> ReadAsString()
        {
            return _response.ReadAsString();
        }

        public Stream GetResponseStream()
        {
            return _response.GetResponseStream();
        }

        public void Close()
        {
            ((IDisposable) _response).Dispose();
        }

        public Exception Exception { get; set; }
    }
}