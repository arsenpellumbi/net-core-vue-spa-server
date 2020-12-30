using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ReverseProxyApplication
{
    public class ReverseProxyMiddleware
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly RequestDelegate _nextMiddleware;
        private readonly IConfiguration _configuration;

        public ReverseProxyMiddleware(IConfiguration configuration, RequestDelegate nextMiddleware)
        {
            _nextMiddleware = nextMiddleware;
            _configuration = configuration;
        }

        public async Task Invoke(HttpContext context)
        {
            var targetUri = BuildTargetUri(context.Request);

            if (targetUri != null)
            {
                var targetRequestMessage = CreateTargetMessage(context, targetUri);

                using (var responseMessage = await _httpClient.SendAsync(targetRequestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted))
                {
                    context.Response.StatusCode = (int)responseMessage.StatusCode;

                    CopyFromTargetResponseHeaders(context, responseMessage);

                    await ProcessResponseContent(context, responseMessage);
                }

                return;
            }

            await _nextMiddleware(context);
        }

        private async Task ProcessResponseContent(HttpContext context, HttpResponseMessage responseMessage)
        {
            if(context.Response.StatusCode == StatusCodes.Status204NoContent)
            {
                return;
            }

            var content = await responseMessage.Content.ReadAsByteArrayAsync();

            if (IsContentOfType(responseMessage, "text/html") || IsContentOfType(responseMessage, "text/javascript"))
            {
                var stringContent = Encoding.UTF8.GetString(content);
                var reverseProxyEndpoints = this.GetReverseProxyEndpoints();

                foreach(var endpoint in reverseProxyEndpoints)
                {
                    stringContent = stringContent.Replace(endpoint.Value, endpoint.Key);
                }

                await context.Response.WriteAsync(stringContent, Encoding.UTF8);
            } else
            {
                await context.Response.Body.WriteAsync(content);
            }
        }

        private bool IsContentOfType(HttpResponseMessage responseMessage, string type)
        {
            var result = false;

            if (responseMessage.Content?.Headers?.ContentType != null)
            {
                result = responseMessage.Content.Headers.ContentType.MediaType == type;
            }

            return result;
        }

        private HttpRequestMessage CreateTargetMessage(HttpContext context, Uri targetUri)
        {
            var requestMessage = new HttpRequestMessage();
            CopyFromOriginalRequestContentAndHeaders(context, requestMessage);

            requestMessage.RequestUri = targetUri;
            requestMessage.Headers.Host = targetUri.Host;
            requestMessage.Method = GetMethod(context.Request.Method);
           
            return requestMessage;
        }

        private void CopyFromOriginalRequestContentAndHeaders(HttpContext context, HttpRequestMessage requestMessage)
        {
            var requestMethod = context.Request.Method;

            if (!HttpMethods.IsGet(requestMethod) &&
                !HttpMethods.IsHead(requestMethod) &&
                !HttpMethods.IsDelete(requestMethod) &&
                !HttpMethods.IsTrace(requestMethod))
            {
                var streamContent = new StreamContent(context.Request.Body);
                requestMessage.Content = streamContent;
            }

            foreach (var header in context.Request.Headers)
            {
                requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        private void CopyFromTargetResponseHeaders(HttpContext context, HttpResponseMessage responseMessage)
        {
            foreach (var header in responseMessage.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in responseMessage.Content.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
            context.Response.Headers.Remove("transfer-encoding");
        }
        private static HttpMethod GetMethod(string method)
        {
            if (HttpMethods.IsDelete(method)) return HttpMethod.Delete;
            if (HttpMethods.IsGet(method)) return HttpMethod.Get;
            if (HttpMethods.IsHead(method)) return HttpMethod.Head;
            if (HttpMethods.IsOptions(method)) return HttpMethod.Options;
            if (HttpMethods.IsPost(method)) return HttpMethod.Post;
            if (HttpMethods.IsPut(method)) return HttpMethod.Put;
            if (HttpMethods.IsTrace(method)) return HttpMethod.Trace;
            return new HttpMethod(method);
        }

        private Uri BuildTargetUri(HttpRequest request)
        {
            var reverseProxyEndpoints = this.GetReverseProxyEndpoints();

            foreach(var endpoint in reverseProxyEndpoints)
            {
                if (request.Path.StartsWithSegments(endpoint.Key, out PathString remainingPath))
                {
                    var uriBuilder = new UriBuilder(endpoint.Value + remainingPath);
                    uriBuilder.Query = request.QueryString.ToString();
                    return uriBuilder.Uri;
                }
            }

            return null;
        }

        private Dictionary<string, string> GetReverseProxyEndpoints()
        {
            var reverseProxyEndpoints = new Dictionary<string, string>();

            var clients = this._configuration.GetSection("ReverseProxy:Clients");
            var children = clients.GetChildren();

            foreach (var child in children)
            {
                var endpoint = child.GetValue<string>("Endpoint");
                var targetEndpoint = child.GetValue<string>("TargetEndpoint");
                reverseProxyEndpoints.Add(endpoint, targetEndpoint);
            }

            return reverseProxyEndpoints;
        }
    }
}
