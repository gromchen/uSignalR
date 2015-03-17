using System;
using System.Collections.Generic;
using uTasks;

namespace uSignalR.Http
{
    public interface IHttpClient
    {
        void Initialize(IConnection connection);
        Task<IResponse> Get(string url, Action<IRequest> prepareRequest, bool isLongRunning);
        Task<IResponse> Post(string url, Action<IRequest> prepareRequest, IDictionary<string, string> postData, bool isLongRunning);
    }
}