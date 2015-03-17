using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;
using uSignalR.Http;
using uSignalR.Hubs;
using uSignalR.Transports;
using uTasks;

namespace uSignalR
{
    public interface IConnection
    {
        IEnumerable<string> Groups { get; set; }
        bool IsActive { get; }

        string MessageId { get; set; }
        string GroupsToken { get; }
        IDictionary<string, object> Items { get; }
        string ConnectionId { get; }
        string ConnectionToken { get; }
        string Url { get; }
        string QueryString { get; }
        ConnectionState State { get; }
        IClientTransport Transport { get; }

        bool ChangeState(ConnectionState oldState, ConnectionState newState);

        IDictionary<string, string> Headers { get; }
        ICredentials Credentials { get; set; }
        CookieContainer CookieContainer { get; set; }

        event Action<Exception> Error;
        event Action<string> Received;

        void Stop();
        void Disconnect();
        Task<HubResult> Send(string data);

        void OnReceived(JToken data);
        void OnError(Exception ex);
        void OnReconnected();
        void PrepareRequest(IRequest request);
    }
}