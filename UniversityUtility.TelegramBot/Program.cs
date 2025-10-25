using Microsoft.Extensions.Configuration;
using UniversityUtility.Core.Services;
using UniversityUtility.TelegramBot.Models;
using UniversityUtility.TelegramBot.Services;

namespace UniversityUtility.TelegramBot
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var logger = new ConsoleLogger();

            logger.LogInfo("=================================");
            logger.LogInfo("  UNIVERSITY TELEGRAM BOT");
            logger.LogInfo("=================================");
            logger.LogInfo("");

            // Carica configurazione da config.json
            var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
                          .AddJsonFile("config.json", optional: false, reloadOnChange: true)
              .Build();

            var config = new BotConfiguration();
            configuration.Bind(config);

            // Validazione configurazione
            if (string.IsNullOrEmpty(config.TelegramBot.BotToken) ||
                config.TelegramBot.BotToken == "INSERISCI_QUI_IL_TOKEN_DEL_BOT")
            {
                logger.LogError("Bot Token non configurato!");
                logger.LogInfo("");
                logger.LogInfo("Apri il file 'config.json' e configura:");
                logger.LogInfo("\t1. BotToken: ottienilo da @BotFather su Telegram");
                logger.LogInfo("\t2. ChatId: ottienilo da @userinfobot su Telegram");
                logger.LogInfo("");
                logger.LogInfo("Esempio config.json:");
                logger.LogInfo("{");
                logger.LogInfo("\t\"TelegramBot\": {");
                logger.LogInfo("\t\t\"BotToken\": \"1234567890:ABCdefGHI...\",");
                logger.LogInfo("\t\t\"ChatId\": 123456789");
                logger.LogInfo("\t}");
                logger.LogInfo("}\n");
                logger.LogInfo("");

                Console.ReadKey();
                return;
            }

            if (config.TelegramBot.ChatId == 0)
            {
                logger.LogError("/!\\ Chat ID non configurato!");
                logger.LogInfo("\tCerca @userinfobot su Telegram e ottieni il tuo Chat ID");

                Console.WriteLine("\nPremi un tasto per uscire...");
                Console.ReadKey();
                return;
            }

            var botService = new TelegramBotService(
                config.TelegramBot.BotToken,
                config.TelegramBot.ChatId,
                config,
                logger
            );

            try
            {
                logger.LogSuccess("Configurazione caricata da config.json");
                logger.LogInfo($"Chat ID autorizzata: {config.TelegramBot.ChatId}");

                if (config.University.SaveCredentials &&
                    !string.IsNullOrEmpty(config.University.Username))
                {
                    logger.LogInfo($"Credenziali salvate per: {config.University.Username}");
                }

                logger.LogInfo("");
                logger.LogInfo("Avvio bot...");
                logger.LogInfo("");

                await botService.StartAsync();
            }
            catch (Exception ex)
            {
                logger.LogError($"Errore fatale: {ex.Message}");

                // More detailed error information
                if (ex.InnerException != null)
                {
                    logger.LogError($"Dettagli: {ex.InnerException.Message}");
                }

                logger.LogError("");
                logger.LogInfo("Per maggiori dettagli sull'errore, controlla i messaggi sopra.");

                Console.WriteLine("\nPremi un tasto per uscire...");
                Console.ReadKey();
            }
            finally
            {
                await botService.StopAsync();
            }
        }
    }
}