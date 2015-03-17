using System;
using System.IO;
using uTasks;

namespace uSignalR.Http
{
    public interface IResponse
    {
        Task<string> ReadAsString();

        Stream GetResponseStream();

        void Close();

		Exception Exception { get; set; }
    }
}
