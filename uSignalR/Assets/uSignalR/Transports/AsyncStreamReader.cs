using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using uSignalR.Infrastructure;
using uSignalR.Transports.ServerSentEvents;

namespace uSignalR.Transports
{
    public class AsyncStreamReader
    {
        private readonly ChunkBuffer _buffer;
        private readonly Action _closeCallback;
        private readonly IConnection _connection;
        private readonly Stream _stream;
        private bool _processingBuffer;
        private int _processingQueue;
        private int _reading;

        public AsyncStreamReader(Stream stream, IConnection connection, Action closeCallback)
        {
            _closeCallback = closeCallback;
            _stream = stream;
            _connection = connection;
            _buffer = new ChunkBuffer();
        }

        public bool Reading
        {
            get { return _reading == 1; }
        }

        public void StartReading()
        {
            Debug.WriteLine("StartReading");
            if (Interlocked.Exchange(ref _reading, 1) == 0)
                ReadLoop();
        }

        public void StopReading(bool raiseCloseCallback)
        {
            if (Interlocked.Exchange(ref _reading, 0) == 1
                && raiseCloseCallback)
                _closeCallback();
        }

        private void ReadLoop()
        {
            if (!Reading)
                return;

            var buffer = new byte[1024];

            _stream.ReadAsync(buffer).ContinueWithTask(task =>
            {
                if (task.AggregateException != null)
                {
                    var exception = task.AggregateException.GetBaseException();

                    if (!HttpBasedTransport.IsRequestAborted(exception))
                    {
                        if (!(exception is IOException))
                            _connection.OnError(exception);
                        StopReading(true);
                    }
                    return;
                }

                var read = task.Result;

                // Put chunks in the buffer
                if (read > 0)
                    _buffer.Add(buffer, read);

                // Stop any reading we're doing
                if (read == 0)
                {
                    StopReading(true);
                    return;
                }

                // Keep reading the next set of data
                ReadLoop();

                // If we read less than we wanted or if we filled the buffer, process it
                if (read <= buffer.Length)
                    ProcessBuffer();
            });
        }

        private void ProcessBuffer()
        {
            if (!Reading)
                return;

            if (_processingBuffer)
            {
                // Increment the number of times we should process messages
                _processingQueue++;
                return;
            }

            _processingBuffer = true;

            var total = Math.Max(1, _processingQueue);

            for (var i = 0; i < total; i++)
            {
                if (!Reading)
                    return;
                ProcessChunks();
            }

            if (_processingQueue > 0)
                _processingQueue -= total;

            _processingBuffer = false;
        }

        private void ProcessChunks()
        {
            Debug.WriteLine("ProcessChunks");
            while (Reading && _buffer.HasChunks)
            {
                var line = _buffer.ReadLine();

                // No new lines in the buffer so stop processing
                if (line == null)
                    break;

                if (!Reading)
                    return;

                // Try parsing the sseEvent
                SseEvent sseEvent;
                if (!SseEvent.TryParse(line, out sseEvent))
                    continue;

                if (!Reading)
                    return;

                Debug.WriteLine("SSE READ: " + sseEvent);

                switch (sseEvent.Type)
                {
                    case EventType.Id:
                        _connection.MessageId = sseEvent.Data;
                        break;
                    case EventType.Data:
                        if (!sseEvent.Data.Equals("initialized", StringComparison.OrdinalIgnoreCase))
                        {
                            if (Reading)
                            {
                                // We don't care about timeout messages here since it will just reconnect
                                // as part of being a long running request
                                bool timedOutReceived;
                                bool disconnectReceived;

                                HttpBasedTransport.ProcessResponse(
                                    _connection,
                                    sseEvent.Data,
                                    out timedOutReceived,
                                    out disconnectReceived);

                                if (disconnectReceived)
                                    _connection.Stop();

                                if (timedOutReceived)
                                    return;
                            }
                        }
                        break;
                }
            }
        }
    }
}