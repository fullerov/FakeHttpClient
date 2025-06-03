using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace YourProject.Helpers
{
    public class FakeHttpClient : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;

        public FakeHttpClient(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => _responder(request, cancellationToken);

        public static HttpClient GetClient(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder,
            string baseAddress = "http://test/")
        {
            var handler = new FakeHttpClient(responder);
            return new HttpClient(handler)
            {
                BaseAddress = new Uri(baseAddress)
            };
        }
    }
}
