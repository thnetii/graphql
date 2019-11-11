using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Collections.Generic;

namespace THNETII.GraphQL.Http
{
    /// <summary>
    /// Represents the message body of a GraphQL HTTP response.
    /// </summary>
    public class GraphQLResponse<T>
    {
        /// <summary>
        /// The data returned in the response.
        /// </summary>
        [JsonProperty("data")]
        public T Data { get; protected set; }

        /// <summary>
        /// Gets additional properties received in the response message.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JToken> AdditionalProperties { get; } =
            new Dictionary<string, JToken>();
    }
}
