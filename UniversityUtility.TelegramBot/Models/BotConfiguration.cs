namespace UniversityUtility.TelegramBot.Models
{
    public class BotConfiguration
    {
        public TelegramBotSettings TelegramBot { get; set; } = new();
        public UniversitySettings University { get; set; } = new();
    }

    public class TelegramBotSettings
    {
        public string BotToken { get; set; } = string.Empty;
        public long ChatId { get; set; }
    }

    public class UniversitySettings
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string DefaultSubject { get; set; } = string.Empty;
        public bool SaveCredentials { get; set; }
    }
}
