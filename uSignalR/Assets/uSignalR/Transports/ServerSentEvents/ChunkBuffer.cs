using System;
using System.Text;

namespace uSignalR.Transports.ServerSentEvents
{
    public class ChunkBuffer
    {
        private int _offset;
        private readonly StringBuilder _buffer;
        private readonly StringBuilder _lineBuilder;

        public ChunkBuffer()
        {
            _buffer = new StringBuilder();
            _lineBuilder = new StringBuilder();
        }

        public bool HasChunks
        {
            get
            {
                return _offset < _buffer.Length;
            }
        }

        public string ReadLine()
        {
            // Lock while reading so that we can make safe assumptions about the buffer indices
            lock (_buffer)
            {
                for (int i = _offset; i < _buffer.Length; i++, _offset++)
                {
                    if (_buffer[i] == '\n')
                    {
                        _buffer.Remove(0, _offset + 1);

                        string line = _lineBuilder.ToString();
                    	_lineBuilder.Length = 0;
                        _offset = 0;
                        return line;
                    }
                    _lineBuilder.Append(_buffer[i]);
                }
                return null;
            }
        }

        public void Add(byte[] buffer, int length)
        {
            lock (_buffer)
            {
                _buffer.Append(Encoding.UTF8.GetString(buffer, 0, length));
            }
        }

        public void Add(ArraySegment<byte> buffer)
        {
            _buffer.Append(Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count));
        }
    }
}
