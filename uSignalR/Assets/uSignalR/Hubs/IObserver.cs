namespace uSignalR.Hubs
{
    public interface IObserver<in T>
    {
        void OnNext(T value);
    }
}