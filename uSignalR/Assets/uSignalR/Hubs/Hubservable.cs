using System;

namespace uSignalR.Hubs
{
	public class Hubservable : IObservable<object[]>
	{
		private readonly string _eventName;
		private readonly HubProxy _proxy;

		public Hubservable(HubProxy proxy, string eventName)
		{
			_proxy = proxy;
			_eventName = eventName;
		}

		public IDisposable Subscribe(IObserver<object[]> observer)
		{
			var subscription = _proxy.Subscribe(_eventName);
			subscription.Data += observer.OnNext;

			return new DisposableAction(() =>
			{
				subscription.Data -= observer.OnNext;
			});
		}
	}
}
