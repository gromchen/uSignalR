using System;

namespace uSignalR.Hubs
{
    public interface IObservable<T>
    {
        IDisposable Subscribe(IObserver<T> observer);
    }
}