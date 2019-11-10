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

        /// <summary>
        /// Creates a new GraphQL Exception instance from an error received in
        /// a <see cref="GraphQLResponse"/>.
        /// </summary>
        /// <param name="error">The error received in an GraphQL HTTP response message.</param>
        /// <param name="jsonResponse">The entire JSON serialized response body of the HTTP Response message, if any.</param>
        /// <remarks>
        /// The value of the <see cref="GraphQLError.Message"/> property is used
        /// to initialize the exception <see cref="Exception.Message"/> property.
        /// </remarks>
        public GraphQLException(GraphQLError error, JObject jsonResponse)
            : this(error?.Message!)
        {
            GraphQLError = error;
            JsonResponse = jsonResponse;
            if (!(jsonResponse is null))
            {
                foreach (var key in jsonResponse)
                    Data[key.Value] = key.Key; 
            }
        }

        /// <inheritdoc cref="Exception(SerializationInfo, StreamingContext)" />
        protected GraphQLException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            if (JsonResponse is JObject jsonResponse)
            {
                foreach (var kvp in jsonResponse)
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
