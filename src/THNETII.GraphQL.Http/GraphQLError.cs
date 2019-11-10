using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace THNETII.GraphQL.Http
{
    /// <summary>
	/// Represents the error of a <see cref="GraphQLResponse"/>
	/// </summary>
	public class GraphQLError : IEquatable<GraphQLError>
    {
        /// <summary>
        /// The error message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The Location of an error
        /// </summary>
        public IEnumerable<GraphQLLocation> Locations { get; set; }

        /// <summary>
        /// Additional error entries
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JToken> AdditionalData { get; } =
            new Dictionary<string, JToken>();

        /// <inheritdoc />
        public override bool Equals(object obj) => Equals(obj as GraphQLError);

        /// <inheritdoc />
        public bool Equals(GraphQLError other)
        {
            return other switch
            {
                null => false,
                GraphQLError e when ReferenceEquals(this, e) => true,
                GraphQLError e when Message != e.Message => false,
                GraphQLError e when Locations != e.Locations => false,
                GraphQLError e when !Equals(AdditionalData, e.AdditionalData) => false,
                _ => true,
            };
        }

        /// <inheritdoc />
        public override int GetHashCode() => Message?.GetHashCode() ?? default;

        /// <inheritdoc />
        public static bool operator ==(GraphQLError error1, GraphQLError error2)
        {
            if (error1 is null)
                return error2 is null ? true : false;
            return error1.Equals(error2);
        }

        /// <inheritdoc />
        public static bool operator !=(GraphQLError error1, GraphQLError error2)
        {
            if (error1 is null)
                return error2 is null ? false : true;
            return !error1.Equals(error2);
        }
    }

}
