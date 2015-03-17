using System;
using uSignalR.Http;
using uSignalR.Hubs;
using uTasks;

namespace uSignalR.Transports
{
    public class AutoTransport : IClientTransport
    {
        private readonly IHttpClient _httpClient;
        private readonly IClientTransport[] _transports; // List of transports in fallback order
        private IClientTransport _transport; // Transport that's in use

        public AutoTransport(IHttpClient httpClient)
        {
            _httpClient = httpClient;
            _transports = new IClientTransport[]
            {
                new ServerSentEventsTransport(httpClient),
                new LongPollingTransport(httpClient)
            };
        }

        public string Name
        {
            get
            {
                if (_transport == null)
                {
                    return null;
                }

                return _transport.Name;
            }
        }

        public Task<NegotiationResponse> Negotiate(IConnection connection)
        {
            return HttpBasedTransport.GetNegotiationResponse(_httpClient, connection);
        }

        public void Start(IConnection connection, string connectionData)
        {
            // Resolve the transport
            ResolveTransport(connection, connectionData, 0);
        }

        public Task<HubResult> Send(IConnection connection, string data)
        {
            return _transport.Send(connection, data);
        }

        public void Stop(IConnection connection)
        {
            _transport.Stop(connection);
        }

        private void ResolveTransport(IConnection connection, string data, int index)
        {
            // Pick the current transport
            var transport = _transports[index];

            try
            {
                transport.Start(connection, data);
                _transport = transport;
            }
            catch (Exception)
            {
                var next = index + 1;
                if (next < _transports.Length)
                {
                    // Try the next transport
                    ResolveTransport(connection, data, next);
                }
                else
                {
                    // If there's nothing else to try then just fail
                    throw new NotSupportedException("The transports available were not supported on this client.");
                }
            }
        }
    }
}