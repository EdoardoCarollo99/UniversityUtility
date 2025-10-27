using Microsoft.Playwright;
using UniversityUtility.Models;

namespace UniversityUtility.Services
{
    public class UniversityAutomationService : IAsyncDisposable
    {
        private readonly ILogger _logger;
        private readonly IUserInputService _inputService;
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IPage? _page;

        private const string UniversityUrl = "https://lms.mercatorum.multiversity.click/";
        private const int DefaultTimeout = 6000;

        public UniversityAutomationService(ILogger logger, IUserInputService inputService)
        {
            _logger = logger;
            _inputService = inputService;
        }

        public async Task RunAsync()
        {
            try
            {
                await InitializeBrowserAsync();
                await NavigateToUniversityAsync();

                var credentials = GetUserCredentials();

                await LoginAsync(credentials);
                await HandleWalkmePopupAsync();

                credentials.Subject = _inputService.GetInput("Inserisci la materia di interesse...");

                await NavigateToCourseAsync(credentials.Subject);

                await ResetLessonsSystem();

                await ManageLessonsAndOpenIt();

                await GetAndSeeAllVideoLessons();


            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred: {ex.Message}");
                throw;
            }
        }

        private async Task InitializeBrowserAsync()
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

        private async Task NavigateToUniversityAsync()
        {
            if (_page == null)
                throw new InvalidOperationException("Browser page not initialized");

            _logger.LogSuccess($"Navigating to University Site {UniversityUrl}");
            await _page.GotoAsync(UniversityUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
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

            // Wait for post-login page load
            await Task.Delay(DefaultTimeout);
        }

        private async Task HandleWalkmePopupAsync()
        {
            if (_page == null)
                throw new InvalidOperationException("Browser page not initialized");

            _logger.LogInfo("Verificando presenza di walkme...");

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

        private async Task NavigateToCourseAsync(string subject)
        {
            if (_page == null)
                throw new InvalidOperationException("Browser page not initialized");

            // Prima prova a cercare nella sezione "Da Completare"
            await ClickDaCompletareButtonAsync();

            _logger.LogInfo($"Cercando il corso '{subject}' in 'Da Completare'...");

            try
            {
                var courseButton = _page.Locator(
                    $"//span[contains(normalize-space(.), \"{subject}\")]/ancestor::div[.//a[contains(@href, \"/videolezioni/\")]][1]//a[contains(@href, \"/videolezioni/\")]"
                ).First;

                // Controlla se il corso esiste nella sezione "Da Completare"
                var courseCount = await _page.Locator(
                    $"//span[contains(normalize-space(.), \"{subject}\")]/ancestor::div[.//a[contains(@href, \"/videolezioni/\")]][1]//a[contains(@href, \"/videolezioni/\")]"
                ).CountAsync();

                if (courseCount > 0)
                {
                    // Corso trovato in "Da Completare"
                    _logger.LogSuccess($"Corso '{subject}' trovato in 'Da Completare'");
                    await courseButton.ClickAsync();
                    await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    await Task.Delay(DefaultTimeout);
                    _logger.LogSuccess($"Navigato al corso: {subject}");
                    return;
                }
                else
                {
                    // Corso NON trovato in "Da Completare", prova in "Da Iniziare"
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
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Impossibile trovare o aprire il corso '{subject}': {ex.Message}");
                throw;
            }
        }

        // Aggiungi questo nuovo metodo per cliccare sul bottone "Da Iniziare"
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
            }
            catch (Exception ex)
            {
                _logger.LogError($"Bottone 'Da Iniziare' non trovato: {ex.Message}");
                throw new InvalidOperationException($"Impossibile trovare il filtro 'Da Iniziare': {ex.Message}");
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
                _logger.LogError($"Impossibile chiudere e aprire lessons: {ex.Message}"  );
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
                // Ottieni tutti gli elementi della lista
                var lessonsListGroup = _page.Locator(
                    "//div[contains(@class, \"align-left flex items-center h-full leading-normal font-medium\") and not(contains(.,\"lezioni\"))]"
                );

                // Conta gli elementi trovati
                var lessonsCount = await lessonsListGroup.CountAsync();
                _logger.LogInfo($"Trovate {lessonsCount} lezioni");

                // Itera attraverso ogni elemento e clicca
                for (int i = 0; i < lessonsCount; i++)
                {
                    try
                    {
                        _logger.LogInfo($"Aprendo lezione {i + 1} di {lessonsCount}");

                        // Ottieni l'elemento specifico per indice
                        var lessonElement = lessonsListGroup.Nth(i);

                        // Clicca sull'elemento
                        await lessonElement.ClickAsync();
                        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                        await Task.Delay(2000);

                        _logger.LogSuccess($"Lezione {i + 1} aperta con successo");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Errore nell'apertura della lezione {i + 1}: {ex.Message}");
                        // Continua con la prossima lezione anche se questa fallisce
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

            try
            {
                // Ottieni tutti gli elementi della lista
                var lessonsListGroup = _page.Locator(
                    "//div[contains(@class, \"w-1/12 text-xs md:text-xs\")]"
                );

                // Conta gli elementi trovati
                var lessonsCount = await lessonsListGroup.CountAsync();
                _logger.LogInfo($"Trovate {lessonsCount} lezioni");

                // Itera attraverso ogni elemento e clicca
                for (int i = 0; i < lessonsCount; i++)
                {
                    try
                    {
                        _logger.LogInfo($"Aprendo lezione {i + 1} di {lessonsCount}");

                        // Ottieni l'elemento specifico per indice
                        var lessonElement = lessonsListGroup.Nth(i);

                        string lessonText = await lessonElement.InnerTextAsync();

                        if(lessonText.Replace(" ", "").ToLower().Contains("100"))
                        {
                            _logger.LogInfo($"Lezione {i + 1} è una lezione video giá fatta, procedo a saltarla.");
                            continue;
                        }
                        else
                        {
                            _logger.LogInfo($"Lezione {i + 1} non è una lezione video da fare procedo alla visualizzazione.");
                            
                            await lessonElement.ClickAsync();
                            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                            await Task.Delay(2000);

                            await WaitFinishLesson();

                            _logger.LogSuccess($"Lezione {i + 1} aperta con successo");
                        }
                       
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Errore nell'apertura della lezione {i + 1}: {ex.Message}");
                        // Continua con la prossima lezione anche se questa fallisce
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
        private async Task WaitFinishLesson()
        {
            if (_page == null)
                throw new InvalidOperationException("Browser page not initialized");

            _logger.LogInfo($"Monitoraggio avanzamento lezione...");

            try
            {
                var percentElement = _page.Locator(
                    "//div[contains(@class,\"bg-platform-primary h-1 rounded-full absolute\")]"
                ).First;

                // Ottieni l'attributo style
                var styleAttribute = await percentElement.GetAttributeAsync("style");

                if (string.IsNullOrEmpty(styleAttribute))
                {
                    _logger.LogWarning("Impossibile ottenere lo style dell'elemento");
                    return;
                }

                // Estrai la percentuale di width
                var widthPercentage = ExtractWidthPercentage(styleAttribute);

                if (widthPercentage.HasValue)
                {
                    _logger.LogInfo($"Avanzamento lezione: {widthPercentage.Value}%");

                    // Attendi finché la lezione non è completa
                    while (widthPercentage.Value < 100)
                    {
                        await Task.Delay(10000); // Controlla ogni 10 secondi

                        styleAttribute = await percentElement.GetAttributeAsync("style");
                        widthPercentage = ExtractWidthPercentage(styleAttribute ?? string.Empty);

                        if (widthPercentage.HasValue)
                        {
                            _logger.LogInfo($"Avanzamento lezione: {widthPercentage.Value}%");
                        }
                    }

                    _logger.LogSuccess("Lezione completata al 100%");
                }
                else
                {
                    _logger.LogWarning("Impossibile estrarre la percentuale di width");
                }
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

            // Cerca il pattern "width: XX%" o "width:XX%"
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
        public async ValueTask DisposeAsync()
        {
            if (_browser != null)
            {
                await _browser.CloseAsync();
            }

            _playwright?.Dispose();
        }
    }
}
