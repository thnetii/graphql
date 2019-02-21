using System;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace THNETII.GraphQL.Http
{
    /// <summary>
	/// An exception that contains a <see cref="Http.GraphQLError"/>
	/// </summary>
	[Serializable]
    public class GraphQLException : Exception
    {
        /// <inheritdoc cref="Exception()" />
        public GraphQLException() : base() { }

        /// <inheritdoc cref="Exception(string)" />
        public GraphQLException(string message) : base(message) { }

        /// <inheritdoc cref="Exception(string, Exception)" />
        public GraphQLException(string message, Exception innerException)
            : base(message, innerException) { }

        public GraphQLException(GraphQLError error, JObject jsonResponse)
            : this(error?.Message)
        {
            GraphQLError = error;
            JsonResponse = jsonResponse;
            foreach (var key in jsonResponse)
                Data[key.Value] = key.Key;
        }

        /// <inheritdoc cref="Exception(SerializationInfo, StreamingContext)" />
        protected GraphQLException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            if (JsonResponse is JObject)
            {
                foreach (var kvp in JsonResponse)
                    info.AddValue(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
		/// The complete JSON response that contains the error.
		/// </summary>
		public JObject JsonResponse { get; }

        /// <summary>
		/// The GraphQLError
		/// </summary>
		public GraphQLError GraphQLError { get; }
    }
}
