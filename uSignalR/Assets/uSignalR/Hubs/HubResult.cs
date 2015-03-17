using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace uSignalR.Hubs
{
    /// <summary>
    ///     Represents the result of a hub invocation.
    /// </summary>
    public class HubResult
    {
        /// <summary>
        ///     The callback identifier
        /// </summary>
        [JsonProperty("I")]
        public string Id { get; set; }

        /// <summary>
        ///     The return value of the hub
        /// </summary>
        [JsonProperty("R")]
        public JToken Result { get; set; }

        /// <summary>
        ///     The error message returned from the hub invocation.
        /// </summary>
        [JsonProperty("E")]
        public string Error { get; set; }

        /// <summary>
        ///     Extra error data
        /// </summary>
        [JsonProperty("D")]
        public object ErrorData { get; set; }

        /// <summary>
        ///     The caller state from this hub.
        /// </summary>
        [JsonProperty("S")]
        public IDictionary<string, object> State { get; set; }
    }
}