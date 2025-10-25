using Telegram.Bot;
using Telegram.Bot.Types;
using UniversityUtility.Core.Services;

namespace UniversityUtility.TelegramBot.Services
{
    public class TelegramNotificationService : ITelegramNotificationService
    {
        private readonly TelegramBotClient _botClient;
        private readonly long _chatId;
        private readonly ILogger _logger;

        public TelegramNotificationService(TelegramBotClient botClient, long chatId, ILogger logger)
        {
            _botClient = botClient;
            _chatId = chatId;
            _logger = logger;
        }

        public async Task SendMessageAsync(string message)
        {
            try
            {
                await _botClient.SendMessage(
                   _chatId,
                    message,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown
               );
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore invio messaggio Telegram: {ex.Message}");
            }
        }

        public async Task SendPhotoAsync(byte[] photo, string caption = "")
        {
            try
            {
                using var stream = new MemoryStream(photo);
                await _botClient.SendPhoto(
                    _chatId,
                    InputFile.FromStream(stream, "screenshot.png"),
                    caption: caption
               );
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore invio foto Telegram: {ex.Message}");
            }
        }

        public async Task SendProgressUpdateAsync(string lessonName, decimal percentage)
        {
            var progressBar = GenerateProgressBar(percentage);
            var message = $"*{lessonName}*\n{progressBar} {percentage:F1}%";
            await SendMessageAsync(message);
        }

        private static string GenerateProgressBar(decimal percentage)
        {
            if (percentage < 0) percentage = 0;
            if (percentage > 100) percentage = 100;

            const int barLength = 5; // Meno segmenti per una migliore resa con gli emoji
            const string filledChar = "🟢"; // Cerchio verde pieno
            const string emptyChar = "⚪";  // Cerchio bianco vuoto

            var filledLength = (int)Math.Round(percentage / 100 * barLength);
            var emptyLength = barLength - filledLength;

            var filledPart = string.Concat(Enumerable.Repeat(filledChar, filledLength));
            var emptyPart = string.Concat(Enumerable.Repeat(emptyChar, emptyLength));

            return $"{filledPart}{emptyPart} **{percentage:F0}%**"; // Usa grassetto standard
        }
    }
}
