using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using uSignalR.Http;
using uSignalR.Hubs;
using uSignalR.Infrastructure;
using uSignalR.Transports;
using uTasks;
using UnityEngine;

namespace uSignalR
{
    public class Connection : IConnection
    {
        internal static readonly TimeSpan DefaultAbortTimeout = TimeSpan.FromSeconds(30);

        private static Version _assemblyVersion;

        /// <summary>
        ///     Used to synchronize state changes
        /// </summary>
        private readonly object _stateLock = new object();

        private IClientTransport _transport;

        /// <summary>
        /// Used to synchronize starting and stopping specifically
        /// </summary>
        private readonly object _startLock = new object();

        /// <summary>
        /// Used to ensure we don't write to the Trace TextWriter from multiple threads simultaneously 
        /// </summary>
        private readonly object _traceLock = new object();

        private TextWriter _traceWriter;

        private Task _connectTask;

        /// <summary>
        ///     The default connection state is disconnected
        /// </summary>
        private ConnectionState _state;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Connection" /> class.
        /// </summary>
        /// <param name="url">The url to connect to.</param>
        public Connection(string url)
            : this(url, (string) null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Connection" /> class.
        /// </summary>
        /// <param name="url">The url to connect to.</param>
        /// <param name="queryString">The query string data to pass to the server.</param>
        public Connection(string url, IDictionary<string, string> queryString)
            : this(url, CreateQueryString(queryString))
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Connection" /> class.
        /// </summary>
        /// <param name="url">The url to connect to.</param>
        /// <param name="queryString">The query string data to pass to the server.</param>
        public Connection(string url, string queryString)
        {
            if (url == null)
            {
                throw new ArgumentNullException("url");
            }

            if (url.Contains("?"))
            {
                throw new ArgumentException(
                    "Url cannot contain QueryString directly. Pass QueryString values in using available overload.",
                    "url");
            }

            if (!url.EndsWith("/", StringComparison.Ordinal))
            {
                url += "/";
            }

            Url = url;
            QueryString = queryString;
            Groups = new List<string>();
            Items = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            State = ConnectionState.Disconnected;
            TraceWriter = new DebugTextWriter();
            Headers = new HeaderDictionary(this);
        }

        /// <summary>
        /// Occurs when the <see cref="Connection"/> is stopped.
        /// </summary>
        public event Action Closed;

        public event Action<string> Received;
        public event Action<Exception> Error;
        public CookieContainer CookieContainer { get; set; }

        /// <summary>
        ///     Gets the current <see cref="ConnectionState" /> of the connection.
        /// </summary>
        public ConnectionState State
        {
            get { return _state; }
            private set
            {
                lock (_stateLock)
                {
                    if (_state == value)
                        return;

                    var stateChange = new StateChange(_state, value);
                    _state = value;

                    if (StateChanged != null)
                    {
                        StateChanged(stateChange);
                    }
                }
            }
        }

        public IDictionary<string, string> Headers { get; private set; }
        public ICredentials Credentials { get; set; }
        public IEnumerable<string> Groups { get; set; }
        public string Url { get; private set; }
        public bool IsActive { get; private set; }
        public string MessageId { get; set; }
        public string ConnectionId { get; set; }
        public IDictionary<string, object> Items { get; private set; }
        public string QueryString { get; private set; }

        public IClientTransport Transport
        {
            get
            {
                return _transport;
            }
        }

        public string ConnectionToken { get; set; }
        public string GroupsToken { get; set; }

        /// <summary>
        /// Stops the <see cref="Connection"/> and sends an abort message to the server.
        /// </summary>
        public void Stop()
        {
            Stop(DefaultAbortTimeout);
        }

        /// <summary>
        /// Stops the <see cref="Connection"/> and sends an abort message to the server.
        /// <param name="timeout">The timeout</param>
        /// </summary>
        public void Stop(TimeSpan timeout)
        {
            lock (_startLock)
            {
                // todo: connect task
                // todo: receive queue

                lock (_stateLock)
                {
                    // Do nothing if the connection is offline
                    if (State != ConnectionState.Disconnected)
                    {
                        Trace(TraceLevels.Events, "Stop");

                        Transport.Stop(this);

                        Disconnect();
                    }
                }
            }
        }

        /// <summary>
        /// Stops the <see cref="Connection"/> without sending an abort message to the server.
        /// This function is called after we receive a disconnect message from the server.
        /// </summary>
        void IConnection.Disconnect()
        {
            Disconnect();
        }

        private void Disconnect()
        {
            lock (_stateLock)
            {
                // Do nothing if the connection is offline
                if (State != ConnectionState.Disconnected)
                {
                    // Change state before doing anything else in case something later in the method throws
                    State = ConnectionState.Disconnected;

                    Trace(TraceLevels.StateChanges, "Disconnect");

                    // todo: cancellation token source

                    Trace(TraceLevels.Events, "Closed");

                    // Clear the state for this connection
                    ConnectionId = null;
                    ConnectionToken = null;
                    GroupsToken = null;
                    MessageId = null;

                    // TODO: Do we want to trigger Closed if we are connecting?
                    OnClosed();
                }
            }
        }

        protected virtual void OnClosed()
        {
            if (Closed != null)
            {
                Closed();
            }
        }

        /// <summary>
        /// Sends data asynchronously over the connection.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <returns>A task that represents when the data has been sent.</returns>
        public virtual Task<HubResult> Send(string data)
        {
            if (State == ConnectionState.Disconnected)
            {
                throw new InvalidOperationException("The Start method must be called before data can be sent.");
            }

            if (State == ConnectionState.Connecting)
            {
                throw new InvalidOperationException("The connection has not been established.");
            }

            return Transport.Send(this, data);
        }

        /// <summary>
        /// Sends an object that will be JSON serialized asynchronously over the connection.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <returns>A task that represents when the data has been sent.</returns>
        public Task Send(object value)
        {
            return Send(JsonConvert.SerializeObject(value));
        }

        void IConnection.OnReceived(JToken message)
        {
            OnReceived(message);
        }

        void IConnection.OnError(Exception error)
        {
            if (Error != null)
                Error(error);
        }

        void IConnection.OnReconnected()
        {
            if (Reconnected != null)
                Reconnected();
        }

        void IConnection.PrepareRequest(IRequest request)
        {
            request.UserAgent = CreateUserAgentString("SignalR.Client");
            if (Credentials != null)
                request.Credentials = Credentials;

            if (CookieContainer != null)
                request.CookieContainer = CookieContainer;

            if (Headers.Count > 0)
            {
                request.SetRequestHeaders(Headers);
            }
        }

        /// <summary>
        ///     Occurs when the <see cref="Connection" /> state changes.
        /// </summary>
        public event Action<StateChange> StateChanged;

        public event Action Reconnected;

        /// <summary>
        ///     Starts the <see cref="Connection" />.
        /// </summary>
        /// <returns>A task that represents when the connection has started.</returns>
        public Task Start()
        {
            return Start(new DefaultHttpClient());
        }

        /// <summary>
        ///     Starts the <see cref="Connection" />.
        /// </summary>
        /// <param name="httpClient">The http client</param>
        /// <returns>A task that represents when the connection has started.</returns>
        public Task Start(IHttpClient httpClient)
        {
            return Start(new AutoTransport(httpClient));
        }

        /// <summary>
        ///     Starts the <see cref="Connection" />.
        /// </summary>
        /// <param name="transport">The transport to use.</param>
        /// <returns>A task that represents when the connection has started.</returns>
        public Task Start(IClientTransport transport)
        {
            lock (_startLock)
            {
                if (!ChangeState(ConnectionState.Disconnected, ConnectionState.Connecting))
                {
                    return _connectTask ?? TaskAsyncHelper.Empty;
                }

                IsActive = true;
                _transport = transport;

                _connectTask = Negotiate(transport);
            }

            return _connectTask;
        }

        protected virtual string OnSending()
        {
            return null;
        }

        private Task Negotiate(IClientTransport transport)
        {
            return transport.Negotiate(this)
                .ContinueWithTaskResult(task =>
                {
                    var response = task.Result;

                    VerifyProtocolVersion(response.ProtocolVersion);

                    ConnectionId = response.ConnectionId;
                    ConnectionToken = response.ConnectionToken;

                    var data = OnSending();
                    StartTransport(data);

                    return response;
                });
        }

        private void StartTransport(string data)
        {
            // todo: implement with task
            _transport.Start(this, data);
            ChangeState(ConnectionState.Connecting, ConnectionState.Connected);
        }

        private bool ChangeState(ConnectionState oldState, ConnectionState newState)
        {
            return ((IConnection)this).ChangeState(oldState, newState);
        }

        bool IConnection.ChangeState(ConnectionState oldState, ConnectionState newState)
        {
            lock (_stateLock)
            {
                // If we're in the expected old state then change state and return true
                if (_state == oldState)
                {
                    Trace(TraceLevels.StateChanges, "ChangeState({0}, {1})", oldState, newState);

                    State = newState;
                    return true;
                }
            }

            // Invalid transition
            return false;
        }

        private void VerifyProtocolVersion(string versionString)
        {
            Version version;
            if (String.IsNullOrEmpty(versionString) ||
                !TryParseVersion(versionString, out version) ||
                !(version.Major == 1 && version.Minor == 2))
            {
                throw new InvalidOperationException("Incompatible protocol version.");
            }
        }

        public TraceLevels TraceLevel { get; set; }

        public TextWriter TraceWriter
        {
            get
            {
                return _traceWriter;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _traceWriter = value;
            }
        }

        public void Trace(TraceLevels level, string format, params object[] args)
        {
            lock (_traceLock)
            {
                if ((TraceLevel & level) == level)
                {
                    _traceWriter.WriteLine(
                        DateTime.UtcNow.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture) + " - " +
                            (ConnectionId ?? "null") + " - " +
                            format,
                        args);
                }
            }
        }

        protected virtual void OnReceived(JToken message)
        {
            if (Received != null)
                Received(message.ToString());
        }

        private static string CreateUserAgentString(string client)
        {
            if (_assemblyVersion == null)
                _assemblyVersion = new AssemblyName(typeof (Connection).Assembly.FullName).Version;

            return String.Format(
                CultureInfo.InvariantCulture,
                "{0}/{1} ({2})",
                client,
                _assemblyVersion,
                Environment.OSVersion);
        }

        private static bool TryParseVersion(string versionString, out Version version)
        {
            try
            {
                version = new Version(versionString);
                return true;
            }
            catch (ArgumentException)
            {
                version = new Version();
                return false;
            }
        }

        private static string CreateQueryString(IDictionary<string, string> queryString)
        {
            return String.Join("&", queryString.Select(kvp => kvp.Key + "=" + kvp.Value).ToArray());
        }

        /// <summary>
        /// Default text writer
        /// </summary>
        private class DebugTextWriter : TextWriter
        {
            public DebugTextWriter()
                : base(CultureInfo.InvariantCulture)
            {
            }

            public override void WriteLine(string value)
            {
                Debug.LogError(value);
            }

            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }
        }
    }
}