using System.Collections.Concurrent;
using System.Net;

namespace Shopping.Web.Razor.Tests.Support;

public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly ConcurrentQueue<HttpResponseMessage> _responses;
    private readonly ConcurrentQueue<HttpRequestMessage> _requests = new();

    public FakeHttpMessageHandler(IEnumerable<HttpResponseMessage> responses)
    {
        _responses = new ConcurrentQueue<HttpResponseMessage>(responses);
    }

    public int RequestCount => _requests.Count;

    public IReadOnlyCollection<HttpRequestMessage> Requests => _requests.ToArray();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requests.Enqueue(CloneRequest(request));

        if (_responses.TryDequeue(out var response))
        {
            return Task.FromResult(response);
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{}")
        });
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        if (request.Content is not null)
        {
            var content = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            clone.Content = new StringContent(content);
        }

        return clone;
    }
}
