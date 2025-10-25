namespace UniversityUtility.Core.Services
{
    public interface ITelegramNotificationService
    {
        Task SendMessageAsync(string message);
        Task SendPhotoAsync(byte[] photo, string caption = "");
        Task SendProgressUpdateAsync(string lessonName, decimal percentage);
    }
}
