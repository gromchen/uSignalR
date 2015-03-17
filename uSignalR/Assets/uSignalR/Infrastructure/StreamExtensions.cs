using System.IO;
using uTasks;

namespace uSignalR.Infrastructure
{
    public static class StreamExtensions
    {
        public static Task WriteAsync(this Stream stream, byte[] buffer)
        {
            return TaskFactory.StartNew(() => stream.Write(buffer, 0, buffer.Length));
        }

        public static Task<int> ReadAsync(this Stream stream, byte[] buffer)
        {
            return TaskFactory.StartNew(() => stream.Read(buffer, 0, buffer.Length));
        }
    }
}