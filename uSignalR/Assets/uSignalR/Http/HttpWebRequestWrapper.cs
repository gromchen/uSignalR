using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;

namespace uSignalR.Http
{
    public class HttpWebRequestWrapper : IRequest
    {
        private readonly HttpWebRequest _request;

        private readonly IDictionary<string, Action<HttpWebRequest, string>> _restrictedHeadersSet = new Dictionary
            <string, Action<HttpWebRequest, string>>
        {
            {HttpRequestHeader.Accept.ToString(), (request, value) => { request.Accept = value; }},
            {HttpRequestHeader.ContentType.ToString(), (request, value) => { request.ContentType = value; }},
            {
                HttpRequestHeader.ContentLength.ToString(),
                (request, value) => { request.ContentLength = Int32.Parse(value, CultureInfo.CurrentCulture); }
            },
            {HttpRequestHeader.UserAgent.ToString(), (request, value) => { request.UserAgent = value; }},
            {HttpRequestHeader.Connection.ToString(), (request, value) => { request.Connection = value; }},
            {HttpRequestHeader.Expect.ToString(), (request, value) => { request.Expect = value; }},
            {
                HttpRequestHeader.IfModifiedSince.ToString(),
                (request, value) => { request.IfModifiedSince = DateTime.Parse(value, CultureInfo.CurrentCulture); }
            },
            {HttpRequestHeader.Referer.ToString(), (request, value) => { request.Referer = value; }},
            {HttpRequestHeader.TransferEncoding.ToString(), (request, value) => { request.TransferEncoding = value; }}
        };

        public HttpWebRequestWrapper(HttpWebRequest request)
        {
            _request = request;
        }

        public string UserAgent
        {
            get { return _request.UserAgent; }
            set { _request.UserAgent = value; }
        }

        public ICredentials Credentials
        {
            get { return _request.Credentials; }
            set { _request.Credentials = value; }
        }

        public CookieContainer CookieContainer
        {
            get { return _request.CookieContainer; }
            set { _request.CookieContainer = value; }
        }

        public string Accept
        {
            get { return _request.Accept; }
            set { _request.Accept = value; }
        }

        public void Abort()
        {
            _request.Abort();
        }

        public void SetRequestHeaders(IEnumerable<KeyValuePair<string, string>> headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }

            foreach (var headerEntry in headers)
            {
                if (_restrictedHeadersSet.Keys.Contains(headerEntry.Key) == false)
                {
                    _request.Headers.Add(headerEntry.Key, headerEntry.Value);
                }
                else
                {
                    Action<HttpWebRequest, string> setHeaderAction;

                    if (_restrictedHeadersSet.TryGetValue(headerEntry.Key, out setHeaderAction))
                    {
                        setHeaderAction.Invoke(_request, headerEntry.Value);
                    }
                }
            }
        }
    }
}