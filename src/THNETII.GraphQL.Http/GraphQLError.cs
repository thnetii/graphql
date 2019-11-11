using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Collections.Generic;

namespace THNETII.GraphQL.Http
{
    /// <summary>
	/// Represents the error of a <see cref="GraphQLResponse{T}"/>
	/// </summary>
	public class GraphQLError
    {
        /// <summary>
        /// The name of the JSON property in a GraphQL response that contains
        /// errors if any.
        /// </summary>
        public const string ResponsePropertyName = "errors";

        /// <summary>
        /// The error message
        /// </summary>
        public string Message { get; protected set; }

        /// <summary>
        /// The Location of an error
        /// </summary>
        public IEnumerable<GraphQLLocation> Locations { get; protected set; }

        /// <summary>
        /// Additional error entries
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JToken> AdditionalData { get; } =
            new Dictionary<string, JToken>();
    }

}
