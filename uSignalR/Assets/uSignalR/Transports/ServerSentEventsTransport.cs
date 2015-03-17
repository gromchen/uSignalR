using System;
using System.Diagnostics;
using System.Threading;
using uSignalR.Http;

namespace uSignalR.Transports
{
    public class ServerSentEventsTransport : HttpBasedTransport
    {
        private const string ReaderKey = "sse.reader";

        public ServerSentEventsTransport()
            : this(new DefaultHttpClient())
        {
        }

        public ServerSentEventsTransport(IHttpClient httpClient)
            : base(httpClient, "serverSentEvents")
        {
            ReconnectDelay = TimeSpan.FromSeconds(2);
        }

        /// <summary>
        ///     The time to wait after a connection drops to try reconnecting.
        /// </summary>
        private TimeSpan ReconnectDelay { get; set; }

        protected override void OnStart(IConnection connection, string connectionData)
        {
            OpenConnection(connection, connectionData, false);
        }

        protected override void OnBeforeAbort(IConnection connection)
        {
            // Get the reader from the connection and stop it
            var reader = ConnectionExtensions.GetValue<AsyncStreamReader>(connection, ReaderKey);

            if (reader == null)
                return;
            
            // Stop reading data from the stream
            reader.StopReading(false);

            // Remove the reader
            connection.Items.Remove(ReaderKey);
        }

        private void Reconnect(IConnection connection, string data)
        {
            if (connection.IsActive == false)
                return;

            // Wait for a bit before reconnecting
            // todo: delay on the other thread
            Thread.Sleep(ReconnectDelay);

            // Now attempt a reconnect
            OpenConnection(connection, data, true);
        }

        private void OpenConnection(IConnection connection, string data, bool reconnecting)
        {
            // If we're reconnecting add /connect to the url
            var url = reconnecting
                ? connection.Url
                : connection.Url + "connect";

            url += GetReceiveQueryStringWithGroups(connection, data);
            Debug.WriteLine(string.Format("SSE: GET {0}", url));

            HttpClient.Get(url, request =>
            {
                connection.PrepareRequest(request);
                request.Accept = "text/event-stream";
            }, true).ContinueWithTask(task =>
            {
                var response = task.Result;

                if (response.Exception != null)
                {
                    var exception = response.Exception.GetBaseException();

                    if (!IsRequestAborted(exception))
                    {
                        if (reconnecting)
                        {
                            // Only raise the error event if we failed to reconnect
                            connection.OnError(exception);
                        }
                    }

                    if (reconnecting)
                    {
                        // Retry
                        Reconnect(connection, data);
                    }
                }
                else
                {
                    // Get the response stream and read it for messages
                    var stream = response.GetResponseStream();
                    var reader = new AsyncStreamReader(stream, connection, () =>
                    {
                        response.Close();
                        Reconnect(connection, data);
                    });

                    if (reconnecting)
                        // Raise the reconnect event if the connection comes back up
                        connection.OnReconnected();

                    reader.StartReading();

                    // Set the reader for this connection
                    connection.Items[ReaderKey] = reader;
                }
            });
        }
    }
}