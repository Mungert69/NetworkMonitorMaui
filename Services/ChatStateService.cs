using NetworkMonitor.Maui.Models;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace NetworkMonitor.Maui.Services
{
    public class ChatStateService
    {
        // Audio and UI state
        public bool IsMuted { get; set; } = true;
        public bool IsExpanded { get; set; } = false;
        public bool IsDrawerOpen { get; set; } = false;
        public bool AutoScrollEnabled { get; set; } = true;

        // Processing and loading states
        public bool IsReady { get; set; } = true;
        public int LoadCount { get; set; } = 0;
        public string LoadWarning { get; set; } = "";
        public bool IsProcessing { get; set; } = false;
        public bool IsCallingFunction { get; set; } = false;
        public bool IsLLMBusy { get; set; } = false;
        public bool IsToggleDisabled { get; set; } = false;

        // Message and feedback states
        public string ThinkingDots { get; set; } = "";
        public string CallingFunctionMessage { get; set; } = "Calling function...";
        public bool ShowHelpMessage { get; set; } = false;
        public string HelpMessage { get; set; } = "";
        public string CurrentMessage { get; set; } = "";
        public bool IsDashboard { get; set; }

        private string _sessionId;

        public string SessionId
        {
            get => _sessionId;
            set
            {
                _sessionId = value;
            }
        }
        public SystemMessage Message { get; set; } = new SystemMessage
        {
            Info = "init",
            Success = false,
            Text = "Internal Error"
        };

        // In ChatStateService.cs
        public string LLMFeedback
        {
            get => _llmFeedback;
            set
            {
                _llmFeedback = value;
                _ = NotifyStateChanged();
            }
        }
        private string _llmFeedback = string.Empty;

        public List<ChatHistory> Histories
        {
            get => _histories;
            set
            {
                _histories = value;
                _ = NotifyStateChanged();
            }
        }
        private List<ChatHistory> _histories = new();
        public List<HostLink> LinkData { get; set; } = new List<HostLink>();
        public string LLMRunnerType { get; set; } = "TurboLLM";
        public bool IsHoveringMessages { get; set; } = false;
        public bool IsInputFocused { get; set; } = false;

        // Session management
        public string LLMRunnerTypeRef { get; set; }
        public string OpenMessage { get; set; }
        public bool AutoClickedRef { get; set; } = false;

        private readonly IJSRuntime _jsRuntime;

        public ChatStateService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }


        public async Task Initialize(string initRunnerType)
        {
            LLMRunnerType = initRunnerType;
            LLMRunnerTypeRef = initRunnerType;
            SessionId = await GetSessionId();
        }
        public async Task ClearSession()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "sessionId");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "sessionTimestamp");
            SessionId = await GetSessionId();
        }


        public async Task StoreNewSessionID(string newSessionId)
        {
            SessionId = newSessionId;
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "sessionId", newSessionId);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "sessionTimestamp", DateTime.Now.Ticks.ToString());

        }

        private async Task<string> GetSessionId()
        {
            // Check if we have a recent session in localStorage
            var storedSessionId = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "sessionId");
            var storedTimestamp = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "sessionTimestamp");

            if (!string.IsNullOrEmpty(storedSessionId) && !string.IsNullOrEmpty(storedTimestamp))
            {
                var currentTime = DateTime.Now.Ticks;
                var storedTime = long.Parse(storedTimestamp);
                var oneDayInTicks = TimeSpan.TicksPerDay;

                if (currentTime - storedTime <= oneDayInTicks)
                {
                    return storedSessionId;
                }

                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "sessionId");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "sessionTimestamp");
            }

            // Create new session
            var newSessionId = Guid.NewGuid().ToString();
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "sessionId", newSessionId);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "sessionTimestamp", DateTime.Now.Ticks.ToString());
            return newSessionId;
        }

        public event Func<Task>? OnChange = null; // Changed from Action to Func<Task>


        public async Task NotifyStateChanged()
        {
            if (OnChange != null)
            {
                try
                {
                    await OnChange.Invoke();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in state notification: {ex}");
                }
            }
        }
    }
}
