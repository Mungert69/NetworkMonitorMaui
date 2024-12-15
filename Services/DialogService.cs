namespace NetworkMonitor.Maui.Services;

public interface IDialogService
{
    Task DisplayAlert(string title, string message, string cancel);
    Task<bool> DisplayAlert(string title, string message, string accept, string cancel);
}


public class DialogService : IDialogService
{
    private Page? MainPage => Application.Current?.MainPage;

    public async Task DisplayAlert(string title, string message, string cancel)
    {
        if (MainPage != null)
        {
            if (MainThread.IsMainThread)
            {
                await MainPage.DisplayAlert(title, message, cancel);
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await MainPage.DisplayAlert(title, message, cancel);
                });
            }
        }

    }

    public async Task<bool> DisplayAlert(string title, string message, string accept, string cancel)
    {
        bool result = false;
        if (MainPage != null)
        {
            if (MainThread.IsMainThread)
            {
                result = await MainPage.DisplayAlert(title, message, accept, cancel);
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    result = await MainPage.DisplayAlert(title, message, accept, cancel);
                });
            }
        }
        else return false;
        
       

        return result;
    }
}
