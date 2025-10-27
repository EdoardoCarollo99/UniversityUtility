using System.Text;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UniversityUtility.Core.Models;
using UniversityUtility.Core.Services;
using UniversityUtility.TelegramBot.Models;

namespace UniversityUtility.TelegramBot.Services
{
    public class TelegramBotService
    {
        private readonly TelegramBotClient _botClient;
        private readonly ILogger _logger;
        private readonly BotConfiguration _config;
        private readonly CancellationTokenSource _cts;
        private UniversityAutomationService? _automationService;
        private Task? _automationTask;
        private long _allowedChatId;
        private UserCredentials? _credentials;

        public TelegramBotService(string botToken, long allowedChatId, BotConfiguration config, ILogger logger)
        {
            _botClient = new TelegramBotClient(botToken);
            _logger = logger;
            _config = config;
            _allowedChatId = allowedChatId;
            _cts = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            _logger.LogInfo("Avvio bot Telegram...");

            try
            {
                var me = await _botClient.GetMe();
                _logger.LogSuccess($"Bot @{me.Username} connesso con successo");
                _logger.LogInfo($"ID Bot: {me.Id}");
                _logger.LogInfo($"Chat ID autorizzata: {_allowedChatId}");

                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = new[] { UpdateType.Message }
                };

                _botClient.StartReceiving(
                    HandleUpdateAsync,
                    HandleErrorAsync,
                    receiverOptions,
                    _cts.Token
                );

                _logger.LogInfo("");
                _logger.LogSuccess("Bot pronto per ricevere comandi");
                _logger.LogInfo("");
                _logger.LogInfo("ISTRUZIONI:");
                _logger.LogInfo("\t1.Apri Telegram e cerca: @" + me.Username);
                _logger.LogInfo("\t2.Clicca su Start o invia /start al bot");
                _logger.LogInfo("\t3.Poi usa i comandi disponibili");
                _logger.LogInfo("");

                try
                {
                    await SendWelcomeMessage(_allowedChatId, _cts.Token);
                    _logger.LogSuccess("Messaggio di benvenuto inviato su Telegram");
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException apiEx) when (apiEx.Message.Contains("chat not found"))
                {
                    _logger.LogInfo("Messaggio di benvenuto non inviato (chat non ancora avviata)");
                    _logger.LogInfo("Apri Telegram e invia /start al bot per iniziare");
                }

                _logger.LogInfo("");

                await Task.Delay(Timeout.Infinite, _cts.Token);
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException apiEx)
            {
                _logger.LogError($"Errore API Telegram: {apiEx.Message}");
                _logger.LogError("");

                if (apiEx.Message.Contains("Unauthorized") || apiEx.Message.Contains("401"))
                {
                    _logger.LogError("Il Bot Token non e valido o e stato revocato");
                    _logger.LogInfo("");
                    _logger.LogInfo("Soluzione:");
                    _logger.LogInfo("\t1. Vai su Telegram e cerca @BotFather");
                    _logger.LogInfo("\t2. Invia /token e seleziona il tuo bot");
                    _logger.LogInfo("\t3. Copia il nuovo token");
                    _logger.LogInfo("\t4. Aggiorna config.json con il token corretto");
                }
                else if (apiEx.Message.Contains("Not Found") || apiEx.Message.Contains("404"))
                {
                    _logger.LogError("Bot non trovato! Il token potrebbe essere malformato");
                    _logger.LogInfo("Verifica che il token sia nel formato: 1234567890:ABCdefGHI...");
                }
                else if (apiEx.Message.Contains("chat not found"))
                {
                    _logger.LogError("Chat non trovata");
                    _logger.LogInfo("");
                    _logger.LogInfo("Possibili cause:");
                    _logger.LogInfo("\t1. Non hai ancora avviato una conversazione con il bot");
                    _logger.LogInfo("\t2. Il Chat ID nel config.json potrebbe essere errato");
                    _logger.LogInfo("");
                    _logger.LogInfo("Soluzione:");
                    _logger.LogInfo("\t1. Apri Telegram e cerca il tuo bot");
                    _logger.LogInfo("\t2. Clicca su Start o invia /start");
                    _logger.LogInfo("\t3. Verifica il tuo Chat ID con @userinfobot");
                    _logger.LogInfo("\t4. Aggiorna config.json se necessario");
                }
                else
                {
                    _logger.LogError($"Dettagli: {apiEx.ErrorCode} - {apiEx.Message}");
                }

                throw;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError($"Errore di connessione alla rete: {httpEx.Message}");
                _logger.LogError("");
                _logger.LogInfo("Possibili cause:");
                _logger.LogInfo("\t- Nessuna connessione a Internet");
                _logger.LogInfo("\t- Firewall che blocca la connessione");
                _logger.LogInfo("\t- Proxy non configurato correttamente");
                _logger.LogInfo("\t- Telegram potrebbe essere bloccato nella tua rete");

                throw;
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("Timeout della richiesta - connessione troppo lenta o timeout del server");
                throw;
            }
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message)
                return;

            if (message.Text is not { } messageText)
                return;

            var chatId = message.Chat.Id;

            if (chatId != _allowedChatId)
            {
                await botClient.SendMessage(
                    chatId,
                    "Non sei autorizzato a usare questo bot",
                    cancellationToken: cancellationToken
                );

                return;
            }

            _logger.LogInfo($"Ricevuto: {messageText} da {chatId}");

            try
            {
                await ProcessCommandAsync(chatId, messageText, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore processamento comando: {ex.Message}");

                await botClient.SendMessage(
                    chatId,
                    $"Errore: {ex.Message}",
                    cancellationToken: cancellationToken
                );
            }
        }

        private async Task SendWelcomeMessage(long chatId, CancellationToken cancellationToken)
        {
            var welcomeMessage = new StringBuilder();

            welcomeMessage.AppendLine("🤖 **Bot Università Avviato!**");
            welcomeMessage.AppendLine("Benvenuto. Ecco i comandi disponibili per gestire l'automazione:");
            welcomeMessage.AppendLine();

            welcomeMessage.AppendLine("* /start - _Mostra questo messaggio di benvenuto_");
            welcomeMessage.AppendLine("* /run - _Avvia l'automazione con i parametri_");
            welcomeMessage.AppendLine("* /status - _Controlla lo stato attuale dell'automazione_");
            welcomeMessage.AppendLine("* /screenshot - _Cattura e invia uno screenshot della pagina web_");
            welcomeMessage.AppendLine("* /stop - _Ferma immediatamente l'automazione in corso_");
            welcomeMessage.AppendLine();
            welcomeMessage.AppendLine("---");
            welcomeMessage.AppendLine();

            if (_config.University.SaveCredentials && !string.IsNullOrEmpty(_config.University.Username))
            {
                welcomeMessage.AppendLine("💾 **Credenziali Salvate**");
                welcomeMessage.AppendLine($"Username: `{_config.University.Username}`");
                welcomeMessage.AppendLine($"Materia predefinita: *{_config.University.DefaultSubject}*");
                welcomeMessage.AppendLine();

                welcomeMessage.AppendLine("Comandi per **avviare** l'automazione:");
                welcomeMessage.AppendLine("* `/run` - Avvia con le credenziali e la materia predefinite.");
                welcomeMessage.AppendLine("* `/run <materia>` - Avvia con credenziali salvate, cambiando solo la materia.");
                welcomeMessage.AppendLine("* `/run <username> <password> <materia>` - Usa credenziali *custom* solo per questa esecuzione.");
            }
            else
            {
                welcomeMessage.AppendLine("⚠️ **Nessuna Credenziale Salvata**");
                welcomeMessage.AppendLine("L'automazione può essere avviata solo specificando tutti i parametri.");
                welcomeMessage.AppendLine();
                welcomeMessage.AppendLine("Sintassi: `/run <username> <password> <materia>`");
            }

            await _botClient.SendMessage(
                chatId: chatId,
                text: welcomeMessage.ToString(),
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, // Importante per la formattazione
                cancellationToken: cancellationToken
            );
        }

        private async Task ProcessCommandAsync(long chatId, string messageText, CancellationToken cancellationToken)
        {
            // Divide il messaggio in parti, rimuovendo gli spazi vuoti
            var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            // Converte il primo elemento (il comando) in minuscolo
            var command = parts[0].ToLowerInvariant();

            switch (command)
            {
                case "/start":
                    await SendWelcomeMessage(chatId, cancellationToken);
                    break;

                case "/run":
                    // 1. Controllo: Automazione già in esecuzione
                    if (_automationTask != null && !_automationTask.IsCompleted)
                    {
                        await _botClient.SendMessage(
                            chatId,
                            "⚠️ **Automazione già in esecuzione.** Usa `/stop` per fermarla prima.",
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                            cancellationToken: cancellationToken
                        );
                        return;
                    }

                    // 2. Tenta di determinare le credenziali
                    var args = parts.Skip(1).ToArray(); // Argomenti del comando /run

                    // Variabile per memorizzare le credenziali o l'errore
                    UserCredentials credentials = null;
                    string errorMessage = null;

                    // Condizione per verificare se ci sono credenziali salvate nel config
                    var hasSavedCredentials = _config.University.SaveCredentials && !string.IsNullOrEmpty(_config.University.Username);

                    if (hasSavedCredentials)
                    {
                        if (args.Length == 0) // Caso: /run (usa tutto salvato)
                        {
                            if (string.IsNullOrEmpty(_config.University.DefaultSubject))
                            {
                                errorMessage = "❌ **Materia predefinita mancante.** Specifica la materia con `/run <materia>` o aggiorna la configurazione.";
                            }
                            else
                            {
                                credentials = new UserCredentials
                                {
                                    Username = _config.University.Username,
                                    Password = _config.University.Password,
                                    Subject = _config.University.DefaultSubject
                                };
                            }
                        }
                        else if (args.Length == 1) // Caso: /run <materia> (usa user/pass salvati)
                        {
                            credentials = new UserCredentials
                            {
                                Username = _config.University.Username,
                                Password = _config.University.Password,
                                Subject = args[0]
                            };
                        }
                        else if (args.Length >= 3) // Caso: /run <username> <password> <materia...>
                        {
                            credentials = new UserCredentials
                            {
                                Username = args[0],
                                Password = args[1],
                                Subject = string.Join(" ", args.Skip(2))
                            };
                        }
                        else // Formato non supportato con credenziali salvate
                        {
                            errorMessage = "❌ **Formato `/run` non valido.** Formati supportati:\n" +
                                           "* `/run`\n* `/run <materia>`\n* `/run <user> <pass> <materia>`";
                        }
                    }
                    else // Nessuna credenziale salvata - Richiede sempre user, pass e materia
                    {
                        if (args.Length >= 3) // Caso: /run <username> <password> <materia...>
                        {
                            credentials = new UserCredentials
                            {
                                Username = args[0],
                                Password = args[1],
                                Subject = string.Join(" ", args.Skip(2))
                            };
                        }
                        else // Formato insufficiente
                        {
                            errorMessage = "❌ **Credenziali non salvate.** Devi fornire tutti i parametri:\n" +
                                           "Sintassi corretta: `/run <username> <password> <materia>`";
                        }
                    }

                    // 3. Gestione errore o avvio
                    if (errorMessage != null)
                    {
                        await _botClient.SendMessage(
                            chatId,
                            errorMessage,
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                            cancellationToken: cancellationToken
                        );
                        return;
                    }

                    // Avvia automazione con le credenziali determinate
                    _credentials = credentials;
                    await StartAutomationAsync(chatId, cancellationToken);
                    break;

                case "/status":
                    await SendStatusAsync(chatId, cancellationToken);
                    break;

                case "/screenshot":
                    await SendScreenshotAsync(chatId, cancellationToken);
                    break;

                case "/stop":
                    await StopAutomationAsync(chatId, cancellationToken);
                    break;

                default:
                    // Risposta per comando non riconosciuto
                    var defaultMessage = "❓ **Comando non riconosciuto.**\n\n" +
                                         "Comandi disponibili:\n" +
                                         "* `/start` - Mostra aiuto e comandi\n" +
                                         "* `/run` - Avvia automazione\n" +
                                         "* `/status` - Stato corrente\n" +
                                         "* `/screenshot` - Screenshot\n" +
                                         "* `/stop` - Ferma automazione";

                    await _botClient.SendMessage(
                        chatId,
                        defaultMessage,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                        cancellationToken: cancellationToken
                    );
                    break;
            }
        }
        private async Task StartAutomationAsync(long chatId, CancellationToken cancellationToken)
        {
            var logger = new ConsoleLogger();
            var inputService = new TelegramInputService(logger);
            var notificationService = new TelegramNotificationService(_botClient, chatId, logger);

            _automationService = new UniversityAutomationService(logger, inputService, notificationService);

            var startMessage = "🚀 Automazione avviata";
            if (_credentials != null)
            {
                startMessage += $"\n\n👤 Utente: {_credentials.Username}\n📚 Materia: {_credentials.Subject}";
            }

            await _botClient.SendMessage(
                chatId,
                startMessage,
                cancellationToken: cancellationToken
            );

            // Avvia l'automazione in un task separato
            _automationTask = Task.Run(async () =>
          {
              try
              {
                  await _automationService.RunAsync(_credentials);
              }
              catch (OperationCanceledException)
              {
                  _logger.LogInfo("Automazione interrotta dall'utente");
                  await notificationService.SendMessageAsync("⏹️ Automazione interrotta dall'utente");
              }
              catch (TimeoutException tex)
              {
                  _logger.LogError($"Timeout automazione: {tex.Message}");
                  // Screenshot già inviato dal WaitFinishLesson
                  await notificationService.SendMessageAsync($"⏱️ Timeout: {tex.Message}");
              }
              catch (Exception ex)
              {
                  _logger.LogError($"Errore automazione: {ex.Message}");
                  // Screenshot già inviato dal RunAsync
                  await notificationService.SendMessageAsync($"❌ Errore: {ex.Message}");
              }
              finally
              {
                  // Cleanup è già gestito dal RunAsync finally block
                  _logger.LogInfo("Task automazione terminato");
              }
          }, cancellationToken);
        }

        private async Task SendStatusAsync(long chatId, CancellationToken cancellationToken)
        {
            if (_automationService == null)
            {
                await _botClient.SendMessage(
              chatId,
                       "Nessuna automazione attiva",
              cancellationToken: cancellationToken
                  );
                return;
            }

            var status = _automationService.GetCurrentStatus();
            var taskStatus = _automationTask?.Status.ToString() ?? "Non avviato";

            await _botClient.SendMessage(
           chatId,
               $"{status}\n\nTask: {taskStatus}",
             cancellationToken: cancellationToken
                    );
        }

        private async Task SendScreenshotAsync(long chatId, CancellationToken cancellationToken)
        {
            if (_automationService == null)
            {
                await _botClient.SendMessage(
              chatId,
          "Nessuna automazione attiva",
                cancellationToken: cancellationToken
                );
                return;
            }

            var screenshot = await _automationService.GetScreenshotAsync();
            if (screenshot == null)
            {
                await _botClient.SendMessage(
                chatId,
                    "Impossibile ottenere lo screenshot",
                      cancellationToken: cancellationToken
                            );
                return;
            }

            using var stream = new MemoryStream(screenshot);
            await _botClient.SendPhoto(
           chatId,
              InputFile.FromStream(stream, "screenshot.png"),
               caption: $"Screenshot - {DateTime.Now:HH:mm:ss}",
               cancellationToken: cancellationToken
                     );
        }

        private async Task StopAutomationAsync(long chatId, CancellationToken cancellationToken)
        {
            if (_automationService == null || _automationTask == null)
            {
                await _botClient.SendMessage(
                         chatId,
                     "Nessuna automazione da fermare",
                     cancellationToken: cancellationToken
                       );
                return;
            }

            await _botClient.SendMessage(
         chatId,
           "⏹️ Fermando automazione...",
           cancellationToken: cancellationToken
        );

            try
            {
                // Richiedi l'interruzione dell'automazione
                _automationService.RequestStop();

                // Attendi che il task finisca (max 30 secondi)
                var timeoutTask = Task.Delay(30000, cancellationToken);
                var completedTask = await Task.WhenAny(_automationTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _logger.LogWarning("Timeout durante l'interruzione, forzo la chiusura");
                }

                // Disponi l'automazione
                await _automationService.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore durante lo stop: {ex.Message}");
            }
            finally
            {
                _automationService = null;
                _automationTask = null;

                await _botClient.SendMessage(
          chatId,
          "✅ Automazione fermata. Usa /run per riavviarla",
                cancellationToken: cancellationToken
             );
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                Telegram.Bot.Exceptions.ApiRequestException apiEx => $"Errore API Telegram: {apiEx.Message}",
                HttpRequestException httpEx => $"Errore connessione: {httpEx.Message}",
                _ => $"Errore bot Telegram: {exception.Message}"
            };

            _logger.LogError(errorMessage);
            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            _cts.Cancel();

            if (_automationService != null)
            {
                await _automationService.DisposeAsync();
            }
        }
    }
}
