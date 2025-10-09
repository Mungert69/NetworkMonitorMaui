using System.Threading;
using NetworkMonitor.Objects;

namespace NetworkMonitor.Processor.Services;

public interface IAuthService
{
    Task<ResultObj> InitializeAsync();
    Task<ResultObj> SendAuthRequestAsync();
    Task<ResultObj> PollForTokenAsync();
    Task<ResultObj> PollForTokenAsync(CancellationToken cancellationToken);
}

public sealed class FakeAuthService : IAuthService
{
    public ResultObj InitializeResult { get; set; } = new() { Success = true, Message = "init" };
    public ResultObj SendResult { get; set; } = new() { Success = true, Message = "send" };
    public ResultObj PollResult { get; set; } = new() { Success = true, Message = "poll" };

    public int InitializeCalls { get; private set; }
    public int SendAuthCalls { get; private set; }
    public int PollCalls { get; private set; }

    public Task<ResultObj> InitializeAsync()
    {
        InitializeCalls++;
        return Task.FromResult(InitializeResult);
    }

    public Task<ResultObj> SendAuthRequestAsync()
    {
        SendAuthCalls++;
        return Task.FromResult(SendResult);
    }

    public Task<ResultObj> PollForTokenAsync()
    {
        PollCalls++;
        return Task.FromResult(PollResult);
    }

    public Task<ResultObj> PollForTokenAsync(CancellationToken cancellationToken)
    {
        PollCalls++;
        return Task.FromResult(PollResult);
    }
}
