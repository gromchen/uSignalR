using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using uSignalR.Http;

namespace uSignalR.Transports
{
    public class LongPollingTransport : HttpBasedTransport
    {
        private static readonly TimeSpan ErrorDelay = TimeSpan.FromSeconds(2);

        public LongPollingTransport()
            : this(new DefaultHttpClient())
        {
        }

        public LongPollingTransport(IHttpClient httpClient)
            : base(httpClient, "longPolling")
        {
            ReconnectDelay = TimeSpan.FromSeconds(5);
        }

        public TimeSpan ReconnectDelay { get; set; }

        protected override void OnStart(IConnection connection, string data)
        {
            PollingLoop(connection, data, false);
        }

        private void PollingLoop(IConnection connection, string data, bool raiseReconnect)
        {
            var url = connection.Url;
            var reconnectTokenSource = new CancellationTokenSource();
            var reconnectFired = 0;

            if (connection.MessageId == null)
                url += "connect";
            else if (raiseReconnect)
                url += "reconnect";

            url += GetReceiveQueryString(connection, data);

            Debug.WriteLine(string.Format("LP: {0}", url));

            HttpClient.Post(
                url,
                connection.PrepareRequest,
                new Dictionary<string, string> {{"groups", GetSerializedGroups(connection)}}, true).
                ContinueWithTask(task =>
                {
                    var response = task.Result;

                    // Clear the pending request
                    connection.Items.Remove(HttpRequestKey);

                    var shouldRaiseReconnect = false;
                    var disconnectedReceived = false;

                    try
                    {
                        if (response.Exception != null)
                            return;

                        // If the timeout for the reconnect hasn't fired as yet just fire the 
                        // event here before any incoming messages are processed
                        if (raiseReconnect)
                            FireReconnected(connection, reconnectTokenSource, ref reconnectFired);

                        // Get the response
                        response.ReadAsString().ContinueWithTask(t =>
                        {
                            var raw = t.Result;

                            Debug.WriteLine(string.Format("LP Receive: {0}", raw));

                            if (!String.IsNullOrEmpty(raw))
                                ProcessResponse(connection, raw, out shouldRaiseReconnect, out disconnectedReceived);
                        });
                    }
                    finally
                    {
                        if (disconnectedReceived)
                            connection.Stop();
                        else
                        {
                            if (response.Exception != null)
                            {
                                // Cancel the previous reconnect event
                                reconnectTokenSource.Cancel();

                                // Get the underlying exception
                                var exception = response.Exception.GetBaseException();

                                
                                    // Figure out if the request was aborted
                                    var requestAborted = IsRequestAborted(exception);

                                    // Sometimes a connection might have been closed by the server before we get to write anything
                                    // so just try again and don't raise OnError.
                                    if (!requestAborted && !(exception is IOException))
                                    {
                                        // Raise on error
                                        connection.OnError(exception);

                                        // If the connection is still active after raising the error event wait for 2 seconds
                                        // before polling again so we aren't hammering the server
                                        Thread.Sleep(ErrorDelay);
                                        if (connection.IsActive)
                                        {
                                            PollingLoop(connection, data, raiseReconnect: true);
                                        }
                                    }
                            }
                            else
                            {
                                // Continue polling if there was no error
                                if (connection.IsActive)
                                {
                                    PollingLoop(connection, data, shouldRaiseReconnect);
                                }
                            }
                        }
                    }
                });

            if (!raiseReconnect)
                return;

            Thread.Sleep(ReconnectDelay);

            // Fire the reconnect event after the delay. This gives the 
            FireReconnected(connection, reconnectTokenSource, ref reconnectFired);
        }

        private static void FireReconnected(IConnection connection,
            CancellationTokenSource reconnectTokenSource,
            ref int reconnectedFired)
        {
            if (!reconnectTokenSource.IsCancellationRequested
                && Interlocked.Exchange(ref reconnectedFired, 1) == 0)
                connection.OnReconnected();
        }

        private static bool IsReconnecting(IConnection connection)
        {
            return connection.State == ConnectionState.Reconnecting;
        }
    }
}