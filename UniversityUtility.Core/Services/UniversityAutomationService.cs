using Microsoft.Playwright;
using UniversityUtility.Core.Models;

namespace UniversityUtility.Core.Services
{
    public class UniversityAutomationService : IAsyncDisposable
    {
        private readonly ILogger _logger;
        private readonly IUserInputService _inputService;
        private readonly ITelegramNotificationService? _telegramService;
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IPage? _page;
        private string _currentLesson = string.Empty;
        private string _currentSubject = string.Empty;
        private bool _isRunning = false;
        private CancellationTokenSource? _cancellationTokenSource;

        private const string UniversityUrl = "https://lms.mercatorum.multiversity.click/";
        private const int DefaultTimeout = 6000;
        private const int VideoProgressTimeoutMinutes = 5;

        public UniversityAutomationService(
            ILogger logger,
            IUserInputService inputService,
            ITelegramNotificationService? telegramService = null)
        {
            _logger = logger;
            _inputService = inputService;
            _telegramService = telegramService;
        }

        public async Task<byte[]?> GetScreenshotAsync()
        {
            if (_page == null)
                return null;

            try
            {
                return await _page.ScreenshotAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore durante lo screenshot: {ex.Message}");
                return null;
            }
        }

        public string GetCurrentStatus()
        {
            if (_page == null || !_isRunning)
                return "Bot non attivo";

            return $"Bot attivo\nMateria: {_currentSubject}\nLezione corrente: {_currentLesson}";
        }

        public async Task RunAsync(UserCredentials? credentials = null)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _isRunning = true;

            try
            {
                await NotifyAsync("Avvio automazione universita...");

                await InitializeBrowserAsync();
                await NavigateToUniversityAsync();

                credentials ??= GetUserCredentials();

                await LoginAsync(credentials);
                await HandleWalkmePopupAsync();

                if (string.IsNullOrEmpty(credentials.Subject))
                {
                    credentials.Subject = _inputService.GetInput("Inserisci la materia di interesse...");
                }

                _currentSubject = credentials.Subject;
                await NotifyAsync($"Materia selezionata: {_currentSubject}");

                await NavigateToCourseAsync(credentials.Subject);
                await ResetLessonsSystem();
                await ManageLessonsAndOpenIt();
                await GetAndSeeAllVideoLessons();

                await NotifyAsync("Automazione completata con successo!");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Automazione interrotta dall'utente");
                await NotifyAsync("Automazione fermata dall'utente");

                await CaptureAndSendScreenshotAsync("Automazione fermata");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore durante l'automazione: {ex.Message}");
                await NotifyAsync($"ERRORE: {ex.Message}");

                await CaptureAndSendScreenshotAsync($"Errore: {ex.Message}");

                throw;
            }
            finally
            {
                _isRunning = false;
                await CleanupBrowserAsync();
            }
        }

        private async Task CaptureAndSendScreenshotAsync(string caption)
        {
            try
            {
                var screenshot = await GetScreenshotAsync();
                if (screenshot != null && _telegramService != null)
                {
                    _logger.LogInfo("Invio screenshot dell'errore...");
                    await _telegramService.SendScreenshotAsync(screenshot, caption);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Impossibile inviare screenshot: {ex.Message}");
            }
        }

        private async Task NotifyAsync(string message)
        {
            if (_telegramService != null)
            {
                await _telegramService.SendMessageAsync(message);
            }
        }

        private async Task InitializeBrowserAsync()
        {
            try
            {
                _logger.LogInfo("Starting Playwright...");
                _playwright = await Playwright.CreateAsync();

                _logger.LogInfo("Launching Browser...");
                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = false,
                    Channel = "msedge",
                    Args = new[] { "--mute-audio", "--start-maximized" }
                });

                var context = await _browser.NewContextAsync(new BrowserNewContextOptions
                {
                    ViewportSize = ViewportSize.NoViewport
                });

                _page = await context.NewPageAsync();
                _logger.LogSuccess("Browser Launched Successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore durante l'inizializzazione del browser: {ex.Message}");
                throw;
            }
        }

        private async Task NavigateToUniversityAsync()
        {
            if (_page == null)
                throw new InvalidOperationException("Browser page not initialized");

            try
            {
                _logger.LogSuccess($"Navigating to University Site {UniversityUrl}");
                await _page.GotoAsync(UniversityUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore durante la navigazione: {ex.Message}");
                throw;
            }
        }

        private UserCredentials GetUserCredentials()
        {
            _logger.LogWarning("Richiesta Autenticazione...");

            return new UserCredentials
            {
                Username = _inputService.GetInput("Inserisci il tuo username:"),
                Password = _inputService.GetInput("Inserisci la tua password:")
            };
        }

        private async Task LoginAsync(UserCredentials credentials)
        {
            if (_page == null)
                throw new InvalidOperationException("Browser page not initialized");

            try
            {
                _logger.LogInfo("Inserimento username...");
                var usernameInput = _page.Locator("#username").First;
                await usernameInput.ClearAsync();
                await usernameInput.FillAsync(credentials.Username);

                _logger.LogInfo("Inserimento password...");
                var passwordInput = _page.Locator("#password").First;
                await passwordInput.ClearAsync();
                await passwordInput.FillAsync(credentials.Password);

                _logger.LogInfo("Cliccando sul bottone di login...");
                var loginButton = _page.Locator("//button/span[text()='Accedi']").First;
                await loginButton.ClickAsync();
                await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                await Task.Delay(DefaultTimeout);
                await NotifyAsync("Login effettuato con successo");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore durante il login: {ex.Message}");
                throw;
            }
        }

        private async Task HandleWalkmePopupAsync()
        {
            if (_page == null)
                throw new InvalidOperationException("Browser page not initialized");

            _logger.LogInfo("Verificando presenza di walkme...");

            try
            {
                var walkmeExists = await _page.Locator("div[id*='walkme-visual-design']").CountAsync() > 0;

                if (walkmeExists)
                {
                    try
                    {
                        var popUp = _page.Locator("//*[@id='border-49e0cc4f-5895-5af9-52ab-b19efc02d195']").First;
                        await popUp.ClickAsync();
                        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                        _logger.LogSuccess("Popup walkme chiuso.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Impossibile chiudere popup walkme: {ex.Message}");
                    }
                }
                else
                {
                    _logger.LogInfo("Nessun elemento walkme trovato.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore durante la gestione del popup walkme: {ex.Message}");
            }
        }

        private async Task NavigateToCourseAsync(string subject)
        {
            if (_page == null)
                throw new InvalidOperationException("Browser page not initialized");

            try
            {
                await ClickDaCompletareButtonAsync();

                _logger.LogInfo($"Cercando il corso '{subject}' in 'Da Completare'...");

                var courseCount = await _page.Locator(
                              $"//span[contains(normalize-space(.), \"{subject}\")]/ancestor::div[.//a[contains(@href, \"/videolezioni/\")]][1]//a[contains(@href, \"/videolezioni/\")]"
                     ).CountAsync();

                if (courseCount > 0)
                {
                    _logger.LogSuccess($"Corso '{subject}' trovato in 'Da Completare'");
                    var courseButton = _page.Locator(
                      $"//span[contains(normalize-space(.), \"{subject}\")]/ancestor::div[.//a[contains(@href, \"/videolezioni/\")]][1]//a[contains(@href, \"/videolezioni/\")]"
                 ).First;

                    await courseButton.ClickAsync();
                    await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    await Task.Delay(DefaultTimeout);
                    _logger.LogSuccess($"Navigato al corso: {subject}");
                }
                else
                {
                    _logger.LogWarning($"Corso '{subject}' non trovato in 'Da Completare'");
                    await ClickDaIniziareButtonAsync();

                    _logger.LogInfo($"Cercando il corso '{subject}' in 'Da Iniziare'...");

                    var courseButtonDaIniziare = _page.Locator(
                          $"//span[contains(normalize-space(.), \"{subject}\")]/ancestor::div[.//a[contains(@href, \"/videolezioni/\")]][1]//a[contains(@href, \"/videolezioni/\")]"
                           ).First;

                    await courseButtonDaIniziare.ClickAsync();
                    await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    await Task.Delay(DefaultTimeout);

                    _logger.LogSuccess($"Corso '{subject}' trovato in 'Da Iniziare' e aperto con successo");
                    await NotifyAsync($"Corso trovato in 'Da Iniziare'");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Impossibile trovare o aprire il corso '{subject}': {ex.Message}");
                throw;
            }
        }

        private async Task ResetLessonsSystem()
        {
            if (_page == null)
                throw new InvalidOperationException("Browser page not initialized");

            _logger.LogInfo($"Reset lessons state");

            try
            {
                var lessonsGroup = _page.Locator(
                $"//div[contains(@class, \"align-left flex items-center h-full leading-normal font-medium\") and (contains(.,\"lezioni\"))]"
                    ).First;

                await lessonsGroup.ClickAsync();
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Task.Delay(DefaultTimeout);

                _logger.LogSuccess($"Close lessons");

                await lessonsGroup.ClickAsync();
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Task.Delay(DefaultTimeout);

                _logger.LogSuccess($"Opened Lessons");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Impossibile chiudere e aprire lessons: {ex.Message}");
                throw;
            }
        }

        private async Task ManageLessonsAndOpenIt()
        {
            if (_page == null)
                throw new InvalidOperationException("Browser page not initialized");

            _logger.LogInfo($"Opening all lessons");

            try
            {
                var lessonsListGroup = _page.Locator(
               "//div[contains(@class, \"align-left flex items-center h-full leading-normal font-medium\") and not(contains(.,\"lezioni\"))]"
                   );

                var lessonsCount = await lessonsListGroup.CountAsync();
                _logger.LogInfo($"Trovate {lessonsCount} lezioni");

                for (int i = 0; i < lessonsCount; i++)
                {
                    CheckCancellation();

                    try
                    {
                        _logger.LogInfo($"Aprendo lezione {i + 1} di {lessonsCount}");

                        var lessonElement = lessonsListGroup.Nth(i);

                        await lessonElement.ClickAsync();
                        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                        await Task.Delay(2000);

                        _logger.LogSuccess($"Lezione {i + 1} aperta con successo");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Errore nell'apertura della lezione {i + 1}: {ex.Message}");
                    }
                }

                _logger.LogSuccess($"Completata apertura di tutte le lezioni");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Impossibile gestire le lezioni: {ex.Message}");
                throw;
            }
        }

        private async Task GetAndSeeAllVideoLessons()
        {
            if (_page == null)
                throw new InvalidOperationException("Browser page not initialized");

            _logger.LogInfo($"Get all Video lessons");
            await NotifyAsync("Inizio visualizzazione video lezioni...");

            try
            {
                var lessonsListGroup = _page.Locator(
                             "//div[contains(@class, \"w-1/12 text-xs md:text-xs\")]"
            );

                var lessonsCount = await lessonsListGroup.CountAsync();
                _logger.LogInfo($"Trovate {lessonsCount} lezioni");
                await NotifyAsync($"Trovate {lessonsCount} lezioni totali");

                for (int i = 0; i < lessonsCount; i++)
                {
                    CheckCancellation();

                    try
                    {
                        _logger.LogInfo($"Aprendo lezione {i + 1} di {lessonsCount}");

                        var lessonElement = lessonsListGroup.Nth(i);
                        string lessonText = await lessonElement.InnerTextAsync();

                        if (lessonText.Replace(" ", "").ToLower().Contains("100"))
                        {
                            _logger.LogInfo($"Lezione {i + 1} gia completata al 100%, salto");
                            continue;
                        }
                        else
                        {
                            _currentLesson = $"Lezione {i + 1}/{lessonsCount}";
                            _logger.LogInfo($"Lezione {i + 1} da visualizzare, procedo");
                            await NotifyAsync($"Inizio {_currentLesson}");

                            await lessonElement.ClickAsync();
                            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                            await Task.Delay(2000);

                            await WaitFinishLesson(i + 1, lessonsCount);

                            _logger.LogSuccess($"Lezione {i + 1} completata");
                            await NotifyAsync($"Completata {_currentLesson}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Errore nella lezione {i + 1}: {ex.Message}");
                        await NotifyAsync($"Errore lezione {i + 1}: {ex.Message}");
                        throw;
                    }
                }

                _logger.LogSuccess($"Completate tutte le lezioni");
                await NotifyAsync("Tutte le lezioni completate!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Impossibile gestire le lezioni: {ex.Message}");
                throw;
            }
        }

        private async Task WaitFinishLesson(int lessonNumber, int totalLessons)
        {
            if (_page == null)
                throw new InvalidOperationException("Browser page not initialized");

            _logger.LogInfo($"Monitoraggio avanzamento lezione...");

            try
            {
                var percentElement = _page.Locator(
                              "//div[contains(@class,\"bg-platform-primary h-1 rounded-full absolute\")]"
            ).First;

                var styleAttribute = await percentElement.GetAttributeAsync("style");

                if (string.IsNullOrEmpty(styleAttribute))
                {
                    _logger.LogWarning("Impossibile ottenere lo style dell'elemento");
                    return;
                }

                var widthPercentage = ExtractWidthPercentage(styleAttribute);

                if (widthPercentage.HasValue)
                {
                    _logger.LogInfo($"Avanzamento lezione: {widthPercentage.Value}%");

                    decimal lastNotifiedPercentage = 0;
                    decimal lastPercentage = widthPercentage.Value;
                    DateTime lastProgressTime = DateTime.Now;
                    TimeSpan maxWaitTime = TimeSpan.FromMinutes(VideoProgressTimeoutMinutes);

                    while (widthPercentage.Value < 100)
                    {
                        CheckCancellation();

                        await Task.Delay(10000);

                        styleAttribute = await percentElement.GetAttributeAsync("style");
                        widthPercentage = ExtractWidthPercentage(styleAttribute ?? string.Empty);

                        if (widthPercentage.HasValue)
                        {
                            _logger.LogInfo($"Avanzamento lezione: {widthPercentage.Value}%");

                            if (Math.Abs(widthPercentage.Value - lastPercentage) > 0.01m)
                            {
                                lastPercentage = widthPercentage.Value;
                                lastProgressTime = DateTime.Now;
                            }
                            else
                            {
                                var elapsedTime = DateTime.Now - lastProgressTime;

                                if (elapsedTime >= maxWaitTime)
                                {
                                    var errorMsg = $"TIMEOUT: Nessun progresso video per {VideoProgressTimeoutMinutes} minuti (bloccato al {widthPercentage.Value}%)";
                                    _logger.LogError(errorMsg);
                                    await NotifyAsync(errorMsg);

                                    await CaptureAndSendScreenshotAsync($"Video bloccato al {widthPercentage.Value}%");

                                    throw new TimeoutException(errorMsg);
                                }
                            }

                            if (_telegramService != null && widthPercentage.Value - lastNotifiedPercentage >= 25)
                            {
                                await _telegramService.SendProgressUpdateAsync(
                             $"Lezione {lessonNumber}/{totalLessons}",
                                widthPercentage.Value
                         );
                                lastNotifiedPercentage = widthPercentage.Value;
                            }
                        }
                    }

                    _logger.LogSuccess("Lezione completata al 100%");
                }
                else
                {
                    _logger.LogWarning("Impossibile estrarre la percentuale di width");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore nel monitoraggio della lezione: {ex.Message}");
                throw;
            }
        }

        private static decimal? ExtractWidthPercentage(string styleAttribute)
        {
            if (string.IsNullOrEmpty(styleAttribute))
                return null;

            var match = System.Text.RegularExpressions.Regex.Match(
              styleAttribute,
                  @"width\s*:\s*(\d+(?:\.\d+)?)\s*%",
               System.Text.RegularExpressions.RegexOptions.IgnoreCase
                       );

            if (match.Success && decimal.TryParse(match.Groups[1].Value, out var percentage))
            {
                return percentage;
            }

            return null;
        }

        private async Task ClickDaCompletareButtonAsync()
        {
            if (_page == null)
                throw new InvalidOperationException("Browser page not initialized");

            try
            {
                var daCompletareButton = _page.Locator("//button[text()='Da Completare ']").First;
                await daCompletareButton.ClickAsync();
                await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                await Task.Delay(3000);
                _logger.LogInfo("Filtro 'Da Completare' applicato.");
            }
            catch
            {
                _logger.LogWarning("Bottone 'Da Completare' non trovato.");
            }
        }

        private async Task ClickDaIniziareButtonAsync()
        {
            if (_page == null)
                throw new InvalidOperationException("Browser page not initialized");

            try
            {
                var daIniziareButton = _page.Locator("//button[text()='Da Iniziare ']").First;
                await daIniziareButton.ClickAsync();
                await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                await Task.Delay(3000);
                _logger.LogInfo("Filtro 'Da Iniziare' applicato.");
                await NotifyAsync("Ricerca corso in 'Da Iniziare'...");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Bottone 'Da Iniziare' non trovato: {ex.Message}");
                throw new InvalidOperationException($"Impossibile trovare il filtro 'Da Iniziare': {ex.Message}");
            }
        }

        private void CheckCancellation()
        {
            _cancellationTokenSource?.Token.ThrowIfCancellationRequested();
        }

        public void RequestStop()
        {
            _logger.LogWarning("Richiesta interruzione automazione...");
            _cancellationTokenSource?.Cancel();
        }

        private async Task CleanupBrowserAsync()
        {
            try
            {
                if (_browser != null)
                {
                    _logger.LogInfo("Chiusura browser...");
                    await _browser.CloseAsync();
                    _browser = null;
                }

                if (_playwright != null)
                {
                    _playwright.Dispose();
                    _playwright = null;
                }

                _page = null;
                _logger.LogSuccess("Browser chiuso correttamente");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore durante la chiusura del browser: {ex.Message}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            RequestStop();
            await CleanupBrowserAsync();
        }
    }
}
