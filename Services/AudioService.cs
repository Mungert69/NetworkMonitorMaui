using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace NetworkMonitor.Maui.Services
{
    public class AudioService : IAsyncDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private IJSObjectReference? _jsModule;
        private bool _isInitialized = false;

        public AudioService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        private async Task EnsureInitialized()
        {
            if (!_isInitialized)
            {
                _jsModule = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                    "import", "./js/chatInterop.js");
                _isInitialized = true;
            }
        }

        public async Task PlayAudioSequentially(string audioFile)
        {
            await EnsureInitialized();
            await _jsRuntime.InvokeVoidAsync("chatInterop.playAudio", audioFile);
        }

        public async Task PauseAudio()
        {
            await EnsureInitialized();
            await _jsRuntime.InvokeVoidAsync("chatInterop.pauseAudio");
        }

        public async Task ClearQueue()
        {
            await EnsureInitialized();
            await _jsRuntime.InvokeVoidAsync("chatInterop.clearAudioQueue");
        }

        public async Task StartRecording()
        {
            await EnsureInitialized();
            await _jsRuntime.InvokeVoidAsync("chatInterop.startRecording");
        }

        public async Task StopRecording()
        {
            await EnsureInitialized();
            await _jsRuntime.InvokeVoidAsync("chatInterop.stopRecording");
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_jsModule is not null)
                {
                    await _jsModule.DisposeAsync();
                }
            }
            catch { }

        }

        // Note: Your current chatInterop.js doesn't have transcribeAudio implemented
        // You'll need to either add it to chatInterop.js or remove this method
        public async Task<string> TranscribeAudio(byte[] audioBlob)
        {
            await EnsureInitialized();
            return await _jsRuntime.InvokeAsync<string>(
                "chatInterop.transcribeAudio", audioBlob);
        }
    }
}