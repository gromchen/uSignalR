using System;
using System.IO;
using System.Net;
using uTasks;

namespace uSignalR.Http
{
    public static class ResponseExtensions
    {
        public static Task<string> ReadAsString(this HttpWebResponse response)
        {
            return TaskFactory.StartNew(() =>
            {
                using (var stream = response.GetResponseStream())
                {
                    if (stream == null)
                        throw new NullReferenceException("Response stream is null.");
                    
                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            });
        }
    }
}