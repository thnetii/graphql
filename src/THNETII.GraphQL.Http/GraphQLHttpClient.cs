using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using THNETII.Common;
using THNETII.Networking.Http;

namespace THNETII.GraphQL.Http
{
    public class GraphQLHttpClient : IDisposable
    {
        private class PayloadPoolPolicy : IPooledObjectPolicy<Dictionary<string, JToken>>
        {
            public static PayloadPoolPolicy Default { get; } = new PayloadPoolPolicy();

            public Dictionary<string, JToken> Create() =>
                new Dictionary<string, JToken>(capacity: 3, StringComparer.Ordinal);

            public bool Return(Dictionary<string, JToken> obj)
            {
                obj?.Clear();
                return true;
            }
        }

        private static readonly ObjectPool<Dictionary<string, JToken>> payloadPool =
            new DefaultObjectPool<Dictionary<string, JToken>>(
                new DefaultPooledObjectPolicy<Dictionary<string, JToken>>()
                );

        private readonly HttpClient httpClient;
        private readonly bool disposeClient;

        public GraphQLHttpClient(HttpClient httpClient, bool noDispose = false)
        {
            (this.httpClient, disposeClient) = (httpClient.ThrowIfNull(nameof(httpClient)), !noDispose);
        }

        public GraphQLHttpClient(HttpMessageHandler messageHandler, bool noDispose = false)
            : this(new HttpClient(messageHandler.ThrowIfNull(nameof(messageHandler)), !noDispose), noDispose)
        { }

        public Task<JToken> PostQueryAsync(string endpoint, GraphQLRequest request, string mediaType = HttpWellKnownMediaType.ApplicationJsonUtf8, CancellationToken cancelToken = default) =>
            PostQueryAsync(httpClient.PostAsync, endpoint, request, mediaType, cancelToken);

        public Task<JToken> PostQueryAsync(Uri endpoint, GraphQLRequest request, string mediaType = HttpWellKnownMediaType.ApplicationJsonUtf8, CancellationToken cancelToken = default) =>
            PostQueryAsync(httpClient.PostAsync, endpoint, request, mediaType, cancelToken);

        private async Task<JToken> PostQueryAsync<TEndpoint>(Func<TEndpoint, HttpContent, CancellationToken, Task<HttpResponseMessage>> httpPostAsync, TEndpoint endpoint, GraphQLRequest request, string mediaType = HttpWellKnownMediaType.ApplicationJsonUtf8, CancellationToken cancelToken = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            var payloadDict = payloadPool.Get();
            try
            {
                FillPayloadDictionary(payloadDict, request);
                return await PostQueryAsync(httpPostAsync, endpoint, payloadDict, mediaType, cancelToken).ConfigureAwait(false);
            }
            finally
            {
                payloadPool.Return(payloadDict);
            }
        }

        private async Task<JToken> PostQueryAsync<TEndpoint>(Func<TEndpoint, HttpContent, CancellationToken, Task<HttpResponseMessage>> httpPostAsync, TEndpoint endpoint, Dictionary<string, JToken> payload, string mediaType = HttpWellKnownMediaType.ApplicationJsonUtf8, CancellationToken cancelToken = default)
        {
            using (var payloadContent = CreatePayloadContent())
            using (var httpResponse = await httpPostAsync(endpoint, payloadContent, cancelToken).ConfigureAwait(false))
            {
                return await ProcessHttpResponse(httpResponse, cancelToken)
                    .ConfigureAwait(false);
            }

            StringContent CreatePayloadContent()
            {
                const Formatting payloadFormatting =
#if DEBUG
                Formatting.Indented
#else // !DEBUG
                Formatting.None
#endif // DEBUG
                ;
                var payloadJson = JsonConvert.SerializeObject(payload, payloadFormatting);
                return mediaType.TryNotNullOrWhiteSpace(out string useMediaType)
                    ? new StringContent(payloadJson, Encoding.UTF8, useMediaType)
                    : new StringContent(payloadJson, Encoding.UTF8);
            }
        }

        private static void FillPayloadDictionary(Dictionary<string, JToken> payloadDict, GraphQLRequest request)
        {
            if (request.OperationName.TryNotNullOrWhiteSpace(out string operationName))
                payloadDict[GraphQLRequest.OperationNameFieldName] = operationName;
            if (request.Query.TryNotNullOrWhiteSpace(out string query))
                payloadDict[GraphQLRequest.QueryFieldName] = query;
            if (request.Variables.TryNotNullOrEmpty(out var variables))
                payloadDict[GraphQLRequest.VariablesFieldName] = new JObject(variables);
        }

        private static async Task<JToken> ProcessHttpResponse(HttpResponseMessage httpResponse, CancellationToken cancelToken)
        {
            httpResponse.EnsureSuccessStatusCode();
            using (var responseReader = await httpResponse.Content.ReadAsStreamReaderAsync().ConfigureAwait(false))
            using (var jsonReader = new JsonTextReader(responseReader))
            {
                var jsonResponse = await JObject.LoadAsync(jsonReader, cancelToken).ConfigureAwait(false);
                if (jsonResponse.TryGetValue(GraphQLResponse.ErrorsFieldName, out JToken errorToken))
                    throw new GraphQLException((errorToken as JObject).ToObject<GraphQLError>(), jsonResponse);
                else if (jsonResponse.TryGetValue(GraphQLResponse.DataFieldName, out JToken dataToken))
                    return dataToken;
                return null;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposeClient)
                {
                    httpClient.Dispose();
                }
                disposedValue = true;
            }
        }

        ~GraphQLHttpClient()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
