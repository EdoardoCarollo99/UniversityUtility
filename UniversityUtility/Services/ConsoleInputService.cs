namespace UniversityUtility.Services
{
    public class ConsoleInputService : IUserInputService
    {
        private readonly ILogger _logger;

        public ConsoleInputService(ILogger logger)
        {
            _logger = logger;
        }

        public string GetInput(string prompt)
        {
            _logger.LogRequest(prompt);
            Console.Write(">");
            return Console.ReadLine() ?? string.Empty;
        }
    }
}
