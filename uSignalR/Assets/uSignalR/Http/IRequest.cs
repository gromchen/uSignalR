using System.Collections.Generic;
using System.Net;

namespace uSignalR.Http
{
    public interface IRequest
    {
        string UserAgent { get; set; }
        ICredentials Credentials { get; set; }
        CookieContainer CookieContainer { get; set; }
        string Accept { get; set; }
        void Abort();
        void SetRequestHeaders(IEnumerable<KeyValuePair<string, string>> headers);
    }
}