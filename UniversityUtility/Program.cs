using UniversityUtility.Services;

namespace UniversityUtility
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Setup services
            var logger = new ConsoleLogger();
            var inputService = new ConsoleInputService(logger);
            
            await using var automationService = new UniversityAutomationService(logger, inputService);

            try
            {
                await automationService.RunAsync();
            }
            catch (Exception ex)
            {
                logger.LogError($"Application error: {ex.Message}");
            }

            logger.LogRequest("Press Any Key to Exit...");
            Console.ReadLine();
        }
    }
}