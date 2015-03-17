using uSignalR.Hubs;
using uTasks;

namespace uSignalR.Transports
{
    public interface IClientTransport
    {
        string Name { get; }

        Task<NegotiationResponse> Negotiate(IConnection connection);
        void Start(IConnection connection, string connectionData);
        Task<HubResult> Send(IConnection connection, string data);
        void Stop(IConnection connection);
    }
}