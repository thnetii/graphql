using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using THNETII.Networking.Http;

namespace THNETII.GraphQL.Http
{
    /// <summary>
    /// A GraphQL client that can be used to send GraphQL queries and mutations
    /// to a GraphQL endpoint using HTTP.
    /// </summary>
    public class GraphQLHttpClient : IDisposable
    {
        private delegate Task<HttpResponseMessage> HttpPostFunc<TEndpoint>(
            TEndpoint endpoint, HttpContent content,
            CancellationToken cancelToken
            );

        private readonly HttpClient httpClient;
        private readonly bool disposeClient;

        private readonly HttpPostFunc<string> urlPost;
        private readonly HttpPostFunc<Uri> uriPost;

        /// <summary>
        /// Creates a new GraphQL client using the specified <see cref="HttpClient"/>
        /// to send requests.
        /// </summary>
        /// <param name="httpClient">An HTTP client instance to use for requests.</param>
        /// <param name="noDispose">
        /// If <see langword="true"/> <paramref name="httpClient"/> will not be
        /// disposed when <see cref="GraphQLHttpClient"/> instance is disposed,
        /// so that the <see cref="HttpClient"/> instance can be shared across
        /// the application.
        /// <para>
        /// Defaults to <see langword="false"/>, indicating that the underlying
        /// <see cref="HttpClient"/> will be disposed when this instance is
        /// disposed.
        /// </para>
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> is <see langword="null"/>.</exception>
        public GraphQLHttpClient(HttpClient httpClient, bool noDispose = false)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            disposeClient = !noDispose;

            urlPost = this.httpClient.PostAsync;
            uriPost = this.httpClient.PostAsync;
        }

        /// <summary>
        /// Creates a new GraphQL client using the specified <see cref="HttpMessageHandler"/>
        /// to send requests.
        /// </summary>
        /// <param name="messageHandler">The message handler that the <see cref="HttpClient"/> for this instance should use.</param>
        /// <param name="noDispose">
        /// If <see langword="true"/> <paramref name="messageHandler"/> will not
        /// be disposed when <see cref="GraphQLHttpClient"/> instance is disposed,
        /// so that the handler can be shared across the application.
        /// <para>
        /// Defaults to <see langword="false"/>, indicating that the underlying
        /// <see cref="HttpClient"/> and the specified handler will be disposed
        /// when the constructed <see cref="GraphQLHttpClient"/> instance is
        /// disposed.
        /// </para>
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="messageHandler"/> is <see langword="null"/>.</exception>
        public GraphQLHttpClient(HttpMessageHandler messageHandler, bool noDispose = false)
            : this(new HttpClient(
                messageHandler ?? throw new ArgumentNullException(nameof(messageHandler)),
                disposeHandler: !noDispose
                ))
        { }

        /// <summary>
        /// Sends the the GraphQL request to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The GraphQL endpoint to send the request to.</param>
        /// <param name="request">A <see cref="GraphQLRequest"/> instance containing the query or mutation to process.</param>
        /// <param name="mediaType">The media type to use as the request body MIME type. Defaults to the JSON MIME type.</param>
        /// <param name="cancelToken">An optional cancellation token that allows the request to be cancelled.</param>
        /// <returns>The serialized response data as a JSON Token instance.</returns>
        /// <exception cref="GraphQLException">The response body contained a GraphQL error.</exception>
        public Task<JToken> SendAsync(string endpoint, GraphQLRequest request,
            string mediaType = HttpWellKnownMediaType.ApplicationJsonUtf8,
            CancellationToken cancelToken = default) =>
            SendAsync(urlPost, endpoint, request, mediaType, cancelToken);

        /// <inheritdoc cref="SendAsync(string, GraphQLRequest, string, CancellationToken)"/>
        public Task<JToken> SendAsync(Uri endpoint, GraphQLRequest request,
            string mediaType = HttpWellKnownMediaType.ApplicationJsonUtf8,
            CancellationToken cancelToken = default) =>
            SendAsync(uriPost, endpoint, request, mediaType, cancelToken);

        private async Task<JToken> SendAsync<TEndpoint>(
            HttpPostFunc<TEndpoint> httpPostAsync,
            TEndpoint endpoint, GraphQLRequest request,
            string mediaType = HttpWellKnownMediaType.ApplicationJsonUtf8,
            CancellationToken cancelToken = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            using (var content = CreatePayloadContent(request, mediaType))
            using (
                var response = await httpPostAsync(endpoint, content, cancelToken)
                    .ConfigureAwait(false)
                )
            {
                return await ProcessHttpResponse(response, cancelToken)
                    .ConfigureAwait(false);
            }

            static StringContent CreatePayloadContent(GraphQLRequest request,
                string mediaType = HttpWellKnownMediaType.ApplicationJson)
            {
                const Formatting payloadFormatting =
#if DEBUG
                Formatting.Indented
#else // !DEBUG
                Formatting.None
#endif // DEBUG
                ;
                var payloadJson = JsonConvert.SerializeObject(request, payloadFormatting);
                return string.IsNullOrWhiteSpace(mediaType)
                    ? new StringContent(payloadJson, Encoding.UTF8, HttpWellKnownMediaType.ApplicationJson)
                    : new StringContent(payloadJson, Encoding.UTF8, mediaType);
            }
        }

        private static async Task<JToken> ProcessHttpResponse(HttpResponseMessage httpResponse, CancellationToken cancelToken)
        {
            httpResponse.EnsureSuccessStatusCode();
            using var responseReader = await httpResponse.Content
                .ReadAsStreamReaderAsync().ConfigureAwait(false);
            using var jsonReader = new JsonTextReader(responseReader);
            var jsonResponse = await JObject.LoadAsync(jsonReader, cancelToken)
                .ConfigureAwait(false);
            if (jsonResponse.TryGetValue(GraphQLResponse.ErrorsFieldName, out JToken errorToken))
                throw new GraphQLException((errorToken as JObject).ToObject<GraphQLError>(), jsonResponse);
            else if (jsonResponse.TryGetValue(GraphQLResponse.DataFieldName, out JToken dataToken))
                return dataToken;
            return null;
        }

        #region IDisposable Support
        /// <summary>
        /// Disposes the underlying HTTP client, unless the constructor
        /// arguments specified not dispose the handler.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> if the method was called from within <see cref="Dispose()"/>;
        /// <see langword="false"/> if called from the class destructor.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposeClient)
            {
                httpClient.Dispose();
            }
        }

        /// <summary>
        /// Calls <see cref="Dispose(bool)"/> to ensure proper clean-up of
        /// unmanaged resources.
        /// </summary>
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
