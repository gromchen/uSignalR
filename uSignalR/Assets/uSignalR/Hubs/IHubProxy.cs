using uTasks;

namespace uSignalR.Hubs
{
    public interface IHubProxy
    {
        object this[string name] { get; set; }
        Task Invoke(string method, params object[] args);
        Task<T> Invoke<T>(string method, params object[] args);
        Subscription Subscribe(string eventName);
    }
}