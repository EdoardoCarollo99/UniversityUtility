namespace UniversityUtility.Services
{
    public interface ILogger
    {
        void LogInfo(string message);
        void LogSuccess(string message);
        void LogError(string message);
   void LogWarning(string message);
  void LogRequest(string message);
    }
}
