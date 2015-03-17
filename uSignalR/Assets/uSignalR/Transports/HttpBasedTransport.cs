using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using uSignalR.Http;
using uSignalR.Hubs;
using uTasks;

namespace uSignalR.Transports
{
    public abstract class HttpBasedTransport : IClientTransport
    {
        // The receive query strings
        private const string ReceiveQueryStringWithGroups =
            "?transport={0}&connectionId={1}&messageId={2}&groups={3}&connectionData={4}{5}&connectionToken={6}&groupsToken={7}";

        private const string ReceiveQueryString =
            "?transport={0}&connectionId={1}&messageId={2}&connectionData={3}{4}&connectionToken={5}";

        private const string SendQueryString = "?transport={0}&connectionToken={1}{2}"; // The send query string
        protected const string HttpRequestKey = "http.Request";
        protected readonly IHttpClient HttpClient;
        private readonly string _transportName;

        public HttpBasedTransport(IHttpClient httpClient, string transportName)
        {
            HttpClient = httpClient;
            _transportName = transportName;
        }

        public string Name
        {
            get { return _transportName; }
        }

        public Task<NegotiationResponse> Negotiate(IConnection connection)
        {
            return GetNegotiationResponse(HttpClient, connection);
        }

        public void Start(IConnection connection, string connectionData)
        {
            OnStart(connection, connectionData);
        }

        public Task<HubResult> Send(IConnection connection, string data)
        {
            var url = connection.Url + "send";
            var customQueryString = GetCustomQueryString(connection);

            url += String.Format(
                SendQueryString,
                _transportName,
                Uri.EscapeDataString(connection.ConnectionToken),
                customQueryString);

            var postData = new Dictionary<string, string> {{"data", data}};

            return HttpClient.Post(url, connection.PrepareRequest, postData, false)
                .ThenWithTaskResultAndWaitForInnerResult(response => response.ReadAsString())
                .ThenWithTaskResult(raw =>
                {
                    if (!String.IsNullOrEmpty(raw))
                    {
                        return JsonConvert.DeserializeObject<HubResult>(raw);
                    }

                    return new HubResult
                    {
                        Error = "Response is null"
                    };
                });
            // todo: catch error
        }

        public void Stop(IConnection connection)
        {
            var httpRequest = ConnectionExtensions.GetValue<IRequest>(connection, HttpRequestKey);

            if (httpRequest == null)
                return;

            try
            {
                OnBeforeAbort(connection);
                httpRequest.Abort();
            }
            catch (NotImplementedException)
            {
                // If this isn't implemented then do nothing
            }
        }

        internal static Task<NegotiationResponse> GetNegotiationResponse(IHttpClient httpClient, IConnection connection)
        {
            httpClient.Initialize(connection);

            return httpClient.Get(connection.Url + "negotiate", connection.PrepareRequest, false)
                .ThenWithTaskResultAndWaitForInnerResult(response => response.ReadAsString())
                .ThenWithTaskResult(json =>
                {
                    if (json == null)
                        throw new InvalidOperationException("Server negotiation failed.");

                    return JsonConvert.DeserializeObject<NegotiationResponse>(json);
                });
        }

        protected abstract void OnStart(IConnection connection, string connectionData);

        protected string GetReceiveQueryStringWithGroups(IConnection connection, string data)
        {
            return String.Format(
                ReceiveQueryStringWithGroups,
                _transportName,
                Uri.EscapeDataString(connection.ConnectionId),
                Convert.ToString(connection.MessageId),
                GetSerializedGroups(connection),
                data,
                GetCustomQueryString(connection),
                Uri.EscapeDataString(connection.ConnectionToken),
                connection.GroupsToken);
        }

        protected string GetSerializedGroups(IConnection connection)
        {
            return Uri.EscapeDataString(JsonConvert.SerializeObject(connection.Groups));
        }

        protected string GetReceiveQueryString(IConnection connection, string data)
        {
            return String.Format(
                ReceiveQueryString,
                _transportName,
                Uri.EscapeDataString(connection.ConnectionId),
                Convert.ToString(connection.MessageId),
                data,
                GetCustomQueryString(connection),
                Uri.EscapeDataString(connection.ConnectionToken));
        }

        public static bool IsRequestAborted(Exception exception)
        {
            var webException = exception as WebException;
            return (webException != null && webException.Status == WebExceptionStatus.RequestCanceled);
        }

        protected virtual void OnBeforeAbort(IConnection connection)
        {
        }

        public static void ProcessResponse(IConnection connection, string response, out bool timedOut,
            out bool disconnected)
        {
            timedOut = false;
            disconnected = false;
            Debug.WriteLine("ProcessResponse: " + response);

            if (String.IsNullOrEmpty(response))
                return;

            if (connection.MessageId == null)
                connection.MessageId = null;

            try
            {
                var result = JToken.Parse(response);
                Debug.WriteLine("ProcessResponse: result parsed");

                if (!result.HasValues)
                    return;

                timedOut = result.Value<bool>("TimedOut");
                disconnected = result.Value<bool>("Disconnect");

                if (disconnected)
                    return;

                var messages = result["M"] as JArray;

                if (messages != null)
                {
                    foreach (var message in messages)
                    {
                        try
                        {
                            Debug.WriteLine("ProcessResponse: before invoking OnReceived");
                            connection.OnReceived(message);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("ProcessResponse: exception in OnReceived event '" + ex.Message + "'.");
                            connection.OnError(ex);
                        }
                    }

                    connection.MessageId = result["C"].Value<string>();

                    var transportData = result["T"] as JObject;

                    if (transportData != null)
                    {
                        var groups = (JArray) transportData["G"];
                        if (groups != null)
                        {
                            var groupList = new List<string>();
                            foreach (var groupFromTransport in groups)
                            {
                                groupList.Add(groupFromTransport.Value<string>());
                            }
                            connection.Groups = groupList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Failed to response: {0}", ex));
                connection.OnError(ex);
            }
        }

        private static string GetCustomQueryString(IConnection connection)
        {
            return String.IsNullOrEmpty(connection.QueryString)
                ? ""
                : "&" + connection.QueryString;
        }
    }
}