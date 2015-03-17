using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace uSignalR.Hubs
{
    public class HubConnection : Connection
    {
        private readonly Dictionary<string, HubProxy> _hubs = new Dictionary<string, HubProxy>();
        internal readonly Dictionary<string, Action<HubResult>> _callbacks = new Dictionary<string, Action<HubResult>>();

        public HubConnection(string url)
            : base(GetUrl(url))
        {
        }

        protected override void OnReceived(JToken message)
        {
            var invocation = message.ToObject<HubInvocation>();
            HubProxy hubProxy;

            if (_hubs.TryGetValue(invocation.Hub, out hubProxy))
            {
                if (invocation.State != null)
                {
                    foreach (var state in invocation.State)
                    {
                        hubProxy[state.Key] = state.Value;
                    }
                }
                hubProxy.InvokeEvent(invocation.Method, invocation.Args);
            }
            base.OnReceived(message);
        }

        /// <summary>
        ///     Creates an <see cref="IHubProxy" /> for the hub with the specified name.
        /// </summary>
        /// <param name="hubName">The name of the hub.</param>
        /// <returns>A <see cref="IHubProxy" /></returns>
        public IHubProxy CreateProxy(string hubName)
        {
            if (State != ConnectionState.Disconnected)
                throw new InvalidOperationException("A HubProxy cannot be added after the connection has been started.");

            HubProxy hubProxy;

            if (_hubs.TryGetValue(hubName, out hubProxy))
                return hubProxy;

            hubProxy = new HubProxy(this, hubName);
            _hubs[hubName] = hubProxy;

            return hubProxy;
        }

        protected override string OnSending()
        {
            var data = _hubs.Select(p => new HubRegistrationData
            {
                Name = p.Key
            });

            return JsonConvert.SerializeObject(data);
        }

        protected override void OnClosed()
        {
            ClearInvocationCallbacks("Connection was disconnected before invocation result was received.");
            base.OnClosed();
        }

        private static string GetUrl(string url)
        {
            if (!url.EndsWith("/"))
                url += "/";
            return url + "signalr";
        }

        private void ClearInvocationCallbacks(string error)
        {
            var result = new HubResult();
            result.Error = error;

            lock (_callbacks)
            {
                foreach (var callback in _callbacks.Values)
                {
                    callback(result);
                }

                _callbacks.Clear();
            }
        }
    }
}