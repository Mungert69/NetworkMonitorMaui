using System;
using System.Threading.Tasks;

namespace NetworkMonitor.Maui.Services;

internal sealed class TestDispatcher : IUiDispatcher
{
    public bool IsDispatchRequired => false;

    public int DispatchCalls { get; private set; }
    public int DispatchAsyncCalls { get; private set; }

    public void Dispatch(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        DispatchCalls++;
        action();
    }

    public async Task DispatchAsync(Func<Task> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        DispatchAsyncCalls++;
        await action().ConfigureAwait(false);
    }
}
