using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net;
using uSignalR.Infrastructure;
using uTasks;

namespace uSignalR.Http
{
    internal static class HttpHelper
    {
        public static Task<HttpWebResponse> GetHttpResponseAsync(this HttpWebRequest request)
        {
            try
            {
                return TaskFactory.FromAsync(request.BeginGetResponse,
                    asyncResult => (HttpWebResponse) request.EndGetResponse(asyncResult));
            }
            catch (Exception ex)
            {
                return TaskFactory.FromError<HttpWebResponse>(ex);
            }
        }

        public static Task<Stream> GetHttpRequestStreamAsync(this HttpWebRequest request)
        {
            try
            {
                return TaskFactory.FromAsync<Stream>(request.BeginGetRequestStream, request.EndGetRequestStream);
            }
            catch (Exception ex)
            {
                return TaskFactory.FromError<Stream>(ex);
            }
        }

        public static Task<HttpWebResponse> GetAsync(string url, Action<HttpWebRequest> requestPreparer)
        {
            var request = (HttpWebRequest) WebRequest.Create(url);

            if (requestPreparer != null)
            {
                requestPreparer(request);
            }

            return request.GetHttpResponseAsync();
        }

        public static Task<HttpWebResponse> PostAsync(string url, Action<HttpWebRequest> requestPreparer,
            IDictionary<string, string> postData)
        {
            var request = (HttpWebRequest) WebRequest.Create(url);

            if (requestPreparer != null) requestPreparer(request);

            var buffer = ProcessPostData(postData);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            // Set the content length if the buffer is non-null
            request.ContentLength = buffer != null ? buffer.LongLength : 0;

            if (buffer == null)
            {
                // If there's nothing to be written to the request then just get the response
                return request.GetHttpResponseAsync();
            }

            // Write the post data to the request stream
            /*return request.GetHttpRequestStreamAsync()
                .Then(stream => stream.WriteAsync(buffer).Then(stream.Dispose))
                .Then(() => request.GetHttpResponseAsync());*/

            return request.GetHttpRequestStreamAsync()
                .ThenWithTaskAndWaitForInnerTask(stream =>
                {
                    return stream.WriteAsync(buffer).ThenWithTask(() => stream.Dispose());
                })
                .ThenWithTaskResultAndWaitForInnerResult(() => request.GetHttpResponseAsync());
        }

        private static byte[] ProcessPostData(IDictionary<string, string> postData)
        {
            if (postData == null || postData.Count == 0)
                return null;

            var stringB = new StringBuilder();
            foreach (var pair in postData)
            {
                if (stringB.Length > 0)
                    stringB.Append("&");

                if (String.IsNullOrEmpty(pair.Value))
                    continue;
                stringB.AppendFormat("{0}={1}", pair.Key, UriQueryUtility.UrlEncode(pair.Value));
            }
            return Encoding.UTF8.GetBytes(stringB.ToString());
        }
    }
}