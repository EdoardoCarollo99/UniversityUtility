using UniversityUtility.Core.Services;

namespace UniversityUtility.TelegramBot.Services
{
    public class TelegramInputService : IUserInputService
    {
        private readonly Dictionary<string, string> _responses = new();
        private readonly ILogger _logger;

        public TelegramInputService(ILogger logger)
        {
            _logger = logger;
        }

        public void SetResponse(string key, string value)
        {
            _responses[key] = value;
        }

        public string GetInput(string prompt)
        {
            // Per il bot Telegram, restituisce i valori pre-impostati
            if (_responses.TryGetValue(prompt, out var response))
            {
                return response;
            }

            _logger.LogWarning($"Nessuna risposta trovata per: {prompt}");
            return string.Empty;
        }
    }
}
