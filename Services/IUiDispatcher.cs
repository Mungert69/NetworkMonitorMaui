using System;
using System.Threading.Tasks;

namespace NetworkMonitor.Maui.Services
{
    public interface IUiDispatcher
    {
        bool IsDispatchRequired { get; }
        void Dispatch(Action action);
        Task DispatchAsync(Func<Task> action);
    }
}
