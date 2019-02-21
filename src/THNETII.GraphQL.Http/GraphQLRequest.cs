using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using THNETII.Common;

namespace THNETII.GraphQL.Http
{
    public class GraphQLRequest
    {
        private string operationName;
        private string query;

        [JsonProperty(nameof(operationName), NullValueHandling = NullValueHandling.Ignore)]
        public string OperationName
        {
            get { return operationName; }
            set { operationName = value.NotNullOrWhiteSpace(otherwise: null); }
        }

        [JsonProperty(nameof(query), NullValueHandling = NullValueHandling.Ignore)]
        public string Query
        {
            get { return query; }
            set { query = value.NotNullOrWhiteSpace(otherwise: null); }
        }

        [JsonProperty("variables", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, JToken> Variables { get; set; }
    }
}
