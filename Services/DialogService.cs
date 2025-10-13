namespace NetworkMonitor.Maui.Services;

public interface IDialogService
{
    Task DisplayAlert(string title, string message, string cancel);
    Task<bool> DisplayAlert(string title, string message, string accept, string cancel);
}


public class DialogService : IDialogService
{
    private readonly IUiDispatcher _dispatcher;

    public DialogService(IUiDispatcher? dispatcher = null)
    {
        _dispatcher = dispatcher ?? throw new Exception("Fatal Error : Dispatcher is null");
    }

    private Page? MainPage => Application.Current?.Windows[0].Page;

    public async Task DisplayAlert(string title, string message, string cancel)
    {
        if (MainPage == null)
        {
            return;
        }

        if (!_dispatcher.IsDispatchRequired)
        {
            await MainPage.DisplayAlert(title, message, cancel);
            return;
        }

        await _dispatcher.DispatchAsync(() => MainPage.DisplayAlert(title, message, cancel));
    }

    public async Task<bool> DisplayAlert(string title, string message, string accept, string cancel)
    {
        if (MainPage == null)
        {
            return false;
        }

        if (!_dispatcher.IsDispatchRequired)
        {
            return await MainPage.DisplayAlert(title, message, accept, cancel);
        }

        bool result = false;
        await _dispatcher.DispatchAsync(async () =>
        {
            result = await MainPage.DisplayAlert(title, message, accept, cancel);
        });
        return result;
    }
}
