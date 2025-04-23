namespace NetworkMonitor.Maui.Services
{

    public interface ILLMService
{
    List<string> GetLLMTypes();
    string GetLLMServerUrl(string siteId);
}
    public  class LLMService : ILLMService
    {
        public string GetLLMServerUrl(string siteId)
        {
            // Implement your logic to get the LLM server URL
            return $"wss://devoauth.freenetworkmonitor.click/LLM/llm-stream";
        }

        public  List<string> GetLLMTypes()
        {
            return new List<string> { "TurboLLM", "HugLLM", "TestLLM" };
        }
    }
}