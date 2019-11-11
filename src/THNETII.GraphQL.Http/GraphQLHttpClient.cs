using System;
using System.IO;
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
        private static readonly Func<string, HttpRequestMessage> urlMsgCtor =
            url => new HttpRequestMessage(HttpMethod.Post, url);
        private static readonly Func<Uri, HttpRequestMessage> uriMsgCtor =
            uri => new HttpRequestMessage(HttpMethod.Post, uri);

        private readonly HttpClient httpClient;
        private readonly bool disposeClient;

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
            : this(new HttpClient(messageHandler, disposeHandler: !noDispose))
        { }

        protected static JsonSerializer DefaultSerializer { get; } =
            JsonSerializer.CreateDefault();

        protected virtual JsonSerializer JsonSerializer { get; } =
            DefaultSerializer;

        /// <summary>
        /// Sends the the GraphQL request to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The GraphQL endpoint to send the request to.</param>
        /// <param name="request">A <see cref="GraphQLRequest"/> instance containing the query or mutation to process.</param>
        /// <param name="cancelToken">An optional cancellation token that allows the request to be cancelled.</param>
        /// <returns>The serialized response.</returns>
        /// <exception cref="GraphQLException">The response body contained a GraphQL error.</exception>
        public Task<GraphQLResponse<TData>> SendAsync<TData>(string endpoint, GraphQLRequest request,
            CancellationToken cancelToken = default) =>
            SendAsync<string, TData>(urlMsgCtor, endpoint, request, cancelToken);

        /// <inheritdoc cref="SendAsync(string, GraphQLRequest, CancellationToken)"/>
        public Task<GraphQLResponse<TData>> SendAsync<TData>(Uri endpoint, GraphQLRequest request,
            CancellationToken cancelToken = default) =>
            SendAsync<Uri, TData>(uriMsgCtor, endpoint, request, cancelToken);

        /// <summary>
        /// Serializes the specified GraphQL request into a HTTP request body.
        /// </summary>
        /// <param name="request">The GraphQL request to be sent.</param>
        /// <returns>An <see cref="HttpContent"/> instance to attach as the request body to an <see cref="HttpRequestMessage"/>.</returns>
        protected virtual HttpContent CreateRequestBody(GraphQLRequest request)
        {
            var jsonBuilder = new StringBuilder();
            using (var jsonWriter = new StringWriter(jsonBuilder))
            {
                JsonSerializer.Serialize(jsonWriter, request);
                jsonWriter.Flush();
            }
            return new StringContent(jsonBuilder.ToString(), Encoding.UTF8,
                HttpWellKnownMediaType.ApplicationJson);
        }

        private HttpRequestMessage CreateRequestMessage<TEndpoint>(
            Func<TEndpoint, HttpRequestMessage> reqMsgFactory,
            TEndpoint endpoint, HttpContent content)
        {
            var reqMsg = reqMsgFactory(endpoint);
            ConfigureMessage(reqMsg);
            reqMsg.Content = content;
            return reqMsg;
        }

        /// <summary>
        /// Configures an <see cref="HttpRequestMessage"/> that is about to be
        /// sendt to the GraphQL endpoint. If overridden in an inheriting class,
        /// the derived implementation can set additional headers, or otherwise
        /// modify the request.
        /// </summary>
        /// <param name="reqMsg">The created request message to be sent.</param>
        /// <remarks>
        /// This method does gain access to the request body which is attached
        /// to the request after this method returns.
        /// </remarks>
        protected virtual void ConfigureMessage(HttpRequestMessage reqMsg) { }

        private async Task<GraphQLResponse<TData>> SendAsync<TEndpoint, TData>(
            Func<TEndpoint, HttpRequestMessage> reqMsgFactory,
            TEndpoint endpoint, GraphQLRequest request,
            CancellationToken cancelToken = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            using var content = CreateRequestBody(request);
            using var reqMsg = CreateRequestMessage(reqMsgFactory, endpoint, content);
            using var respMsg = await httpClient.SendAsync(reqMsg).ConfigureAwait(false);
            return await ProcessHttpResponse<TData>(respMsg, cancelToken)
                .ConfigureAwait(false);
        }

        private async Task<GraphQLResponse<T>> ProcessHttpResponse<T>(HttpResponseMessage httpResponse, CancellationToken cancelToken)
        {
            httpResponse.EnsureSuccessStatusCode();
            using var responseReader = await httpResponse.Content
                .ReadAsStreamReaderAsync().ConfigureAwait(false);
            using var jsonReader = new JsonTextReader(responseReader);
            var jsonResponse = await JObject.LoadAsync(jsonReader, cancelToken)
                .ConfigureAwait(false);
            if (jsonResponse.TryGetValue(GraphQLError.ResponsePropertyName, out JToken errorToken))
                throw errorToken switch
                {
                    JArray errorArray => new GraphQLException(errorArray.ToObject<GraphQLError[]>(JsonSerializer), jsonResponse),
                    _ => new GraphQLException(errorToken.ToObject<GraphQLError>(JsonSerializer), jsonResponse)
                };
            return jsonResponse.ToObject<GraphQLResponse<T>>(JsonSerializer);
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
