namespace THNETII.GraphQL.Http
{
    /// <summary>
    /// Provides the field names that are present in a GraphQL HTTP response.
    /// </summary>
    public static class GraphQLResponse
    {
        /// <summary>
        /// The JSON property name containing the data of the response.
        /// </summary>
        public const string DataFieldName = "data";
        /// <summary>
        /// The JSON property name containing errors in the response.
        /// </summary>
        public const string ErrorsFieldName = "errors";
    }
}
