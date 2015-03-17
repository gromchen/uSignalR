using System;
using System.Collections.Generic;
using uTasks;

namespace uSignalR.Http
{
    public class DefaultHttpClient : IHttpClient
    {
        private readonly string _longRunningGroup;
        private readonly string _shortRunningGroup;
        private IConnection _connection;

        public DefaultHttpClient()
        {
            var id = Guid.NewGuid().ToString();
            _shortRunningGroup = "SignalR-short-running-" + id;
            _longRunningGroup = "SignalR-long-running-" + id;
        }

        public void Initialize(IConnection connection)
        {
            _connection = connection;
        }

        public Task<IResponse> Get(string url, Action<IRequest> prepareRequest, bool isLongRunning)
        {
            return HttpHelper.GetAsync(url, request =>
            {
                request.ConnectionGroupName = isLongRunning ? _longRunningGroup : _shortRunningGroup;

                var req = new HttpWebRequestWrapper(request);
                prepareRequest(req);
                PrepareClientRequest(req);
            }).ThenWithTaskResult(response => (IResponse) new HttpWebResponseWrapper(response));
        }

        public Task<IResponse> Post(string url, Action<IRequest> prepareRequest, IDictionary<string, string> postData,
            bool isLongRunning)
        {
            return HttpHelper.PostAsync(url, request =>
            {
                request.ConnectionGroupName = isLongRunning ? _longRunningGroup : _shortRunningGroup;

                var req = new HttpWebRequestWrapper(request);
                prepareRequest(req);
                PrepareClientRequest(req);
            }, postData)
                .ThenWithTaskResult(response => (IResponse) new HttpWebResponseWrapper(response));
        }

        private void PrepareClientRequest(HttpWebRequestWrapper req)
        {
            // todo: add certificates

            if (_connection.CookieContainer != null)
            {
                req.CookieContainer = _connection.CookieContainer;
            }

            if (_connection.Credentials != null)
            {
                req.Credentials = _connection.Credentials;
            }

            // todo: add proxy
        }
    }
}