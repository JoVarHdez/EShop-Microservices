using Grpc.Core;

namespace Discount.Tests.Support;

internal sealed class FakeServerCallContext : ServerCallContext
{
    private readonly Metadata _requestHeaders = new();
    private readonly Metadata _responseTrailers = new();
    private Status _status;
    private WriteOptions? _writeOptions;

    protected override string MethodCore => "test/method";

    protected override string HostCore => "localhost";

    protected override string PeerCore => "peer";

    protected override DateTime DeadlineCore => DateTime.UtcNow.AddMinutes(1);

    protected override Metadata RequestHeadersCore => _requestHeaders;

    protected override CancellationToken CancellationTokenCore => CancellationToken.None;

    protected override Metadata ResponseTrailersCore => _responseTrailers;

    protected override Status StatusCore
    {
        get => _status;
        set => _status = value;
    }

    protected override WriteOptions? WriteOptionsCore
    {
        get => _writeOptions;
        set => _writeOptions = value;
    }

    protected override AuthContext AuthContextCore => new("test", new Dictionary<string, List<AuthProperty>>());

    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options)
    {
        throw new NotSupportedException("Propagation token is not required for these tests.");
    }

    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders)
    {
        return Task.CompletedTask;
    }
}
