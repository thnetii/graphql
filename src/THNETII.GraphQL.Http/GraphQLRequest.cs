using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using THNETII.Common;

namespace THNETII.GraphQL.Http
{
    /// <summary>
	/// Represents a Query that can be fetched to a GraphQL Server.
	/// For more information <see href="http://graphql.org/learn/serving-over-http/#post-request"/>
	/// </summary>
	public class GraphQLRequest : IEquatable<GraphQLRequest>
    {
        /// <summary>
        /// The name of the field containing the value of <see cref="OperationName"/>
        /// in the query string or JSON POST payload.
        /// </summary>
        public const string OperationNameFieldName = "operationName";
        /// <summary>
        /// The name of the field containing the value of <see cref="Query"/>
        /// in the query string or JSON POST payload.
        /// </summary>
        public const string QueryFieldName = "query";
        /// <summary>
        /// The name of the field containing the value of <see cref="Variables"/>
        /// in the query string or JSON POST payload.
        /// </summary>
        public const string VariablesFieldName = "variables";

        private string operationName;
        private string query;

        /// <summary>
		/// If the provided <see cref="Query"/> contains multiple named operations, this specifies which operation should be executed.
		/// </summary>
		[JsonProperty(OperationNameFieldName, NullValueHandling = NullValueHandling.Ignore)]
        public string OperationName
        {
            get { return operationName; }
            set { operationName = value.NotNullOrWhiteSpace(otherwise: null); }
        }

        /// <summary>
		/// The Query
		/// </summary>
		[JsonProperty(QueryFieldName, NullValueHandling = NullValueHandling.Ignore)]
        public string Query
        {
            get { return query; }
            set { query = value.NotNullOrWhiteSpace(otherwise: null); }
        }

        /// <summary>
		/// The Variables
		/// </summary>
		[JsonProperty(VariablesFieldName, NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, JToken> Variables { get; } =
            new Dictionary<string, JToken>(StringComparer.Ordinal);

        /// <inheritdoc />
		public override bool Equals(object obj) => Equals(obj as GraphQLRequest);

        /// <inheritdoc cref="IEquatable{GraphQLRequest}.Equals(GraphQLRequest)" />
		public bool Equals(GraphQLRequest other)
        {
            return other switch
            {
                null => false,
                GraphQLRequest r when Query != r.Query => false,
                GraphQLRequest r when OperationName != r.OperationName => false,
                GraphQLRequest r when Variables != r.Variables => false,
                _ => true,
            };
        }

        /// <inheritdoc />
        public override int GetHashCode() => Query?.GetHashCode() ?? default;

        /// <inheritdoc />
		public static bool operator ==(GraphQLRequest request1, GraphQLRequest request2)
        {
            if (request1 is null)
                return request2 is null ? true : false;
            return request1.Equals(request2);
        }

        /// <inheritdoc />
		public static bool operator !=(GraphQLRequest request1, GraphQLRequest request2)
        {
            if (request1 is null)
                return request2 is null ? false : true;
            return !request1.Equals(request2);
        }
    }
}
