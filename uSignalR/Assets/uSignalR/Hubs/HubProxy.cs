using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using uTasks;

namespace uSignalR.Hubs
{
    public class HubProxy : IHubProxy
    {
        private readonly IConnection _connection;
        private readonly string _hubName;

        private readonly Dictionary<string, object> _state =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, Subscription> _subscriptions =
            new Dictionary<string, Subscription>(StringComparer.OrdinalIgnoreCase);

        public HubProxy(IConnection connection, string hubName)
        {
            _connection = connection;
            _hubName = hubName;
        }

        public object this[string name]
        {
            get
            {
                object value;
                _state.TryGetValue(name, out value);
                return value;
            }
            set { _state[name] = value; }
        }

        public Subscription Subscribe(string eventName)
        {
            if (eventName == null)
                throw new ArgumentNullException("eventName");

            Subscription subscription;

            if (_subscriptions.TryGetValue(eventName, out subscription))
                return subscription;

            subscription = new Subscription();
            _subscriptions.Add(eventName, subscription);

            return subscription;
        }

        public Task Invoke(string method, params object[] args)
        {
            return Invoke<object>(method, args);
        }

        public Task<TResult> Invoke<TResult>(string method, params object[] args)
        {
            if (string.IsNullOrEmpty(method))
                throw new ArgumentException("Method is null or empty.", "method");

            var invocation = new HubInvocation
            {
                Hub = _hubName,
                Method = method,
                Args = args,
                State = _state,
                CallbackId = "1"
            };

            var value = JsonConvert.SerializeObject(invocation);

            var tcs = new TaskCompletionSource<TResult>();

            _connection.Send(value).ContinueWithTask(task =>
            {
                var result = task.Result;

                if (result != null)
                {
                    if (result.Error != null)
                    {
                        // todo: check for hub exception
                        tcs.TrySetException(new InvalidOperationException(result.Error));
                    }
                    else
                    {
                        try
                        {
                            if (result.State != null)
                            {
                                foreach (var pair in result.State)
                                {
                                    this[pair.Key] = pair.Value;
                                }
                            }

                            if (result.Result != null)
                            {
                                // todo: set json serializer
                                tcs.TrySetResult(result.Result.ToObject<TResult>());
                            }
                            else
                            {
                                tcs.TrySetResult(default(TResult));
                            }
                        }
                        catch (Exception exception)
                        {
                            // todo: try set unwrapped exception
                            tcs.TrySetException(exception);
                        }
                    }
                }

                // todo: try set canceled
            });

            return tcs.Task;
        }

        public void InvokeEvent(string eventName, object[] args)
        {
            Subscription eventObj;
            if (_subscriptions.TryGetValue(eventName, out eventObj))
                eventObj.OnData(args);
        }

        public IEnumerable<string> GetSubscriptions()
        {
            return _subscriptions.Keys;
        }
    }
}