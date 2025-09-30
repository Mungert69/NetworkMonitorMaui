using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;

namespace NetworkMonitor.Maui.Services
{
    public class MainThreadDispatcher : IUiDispatcher
    {
        public bool IsDispatchRequired => !MainThread.IsMainThread;

        public void Dispatch(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            if (MainThread.IsMainThread)
            {
                action();
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(action);
            }
        }

        public Task DispatchAsync(Func<Task> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            if (MainThread.IsMainThread)
            {
                return action();
            }

            var tcs = new TaskCompletionSource<object?>();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await action().ConfigureAwait(false);
                    tcs.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            return tcs.Task;
        }
    }
}
