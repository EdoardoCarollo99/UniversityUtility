using Microsoft.Playwright;

namespace UniversityUtility
{
    internal class Program
    {
        public static IPlaywright Driver { get; set; } = default!;
        public static IBrowser Browser { get; set; } = default!;
        public static string Username { get; set; } = string.Empty;
        public static string Materia { get; set; } = string.Empty;
        public static string Password { get; set; } = string.Empty;

        static async Task Main(string[] args)
        {

            try
            {
                LogInfo("Starting Playwright...");

                Driver = await Playwright
                    .CreateAsync()
                        .ConfigureAwait(true);

                LogInfo("Launching Browser...");

                Browser = await Driver
                    .Chromium
                        .LaunchAsync(new BrowserTypeLaunchOptions
                        {
                            Headless = false,
                            Channel = "msedge",  // Usa Microsoft Edge installato
                            Args = new[] { "--mute-audio" }
                        }).ConfigureAwait(true);

                var page = await Browser
                    .NewPageAsync()
                        .ConfigureAwait(true);

                //await page.SetViewportSizeAsync(1920, 1080);

                LogSuccess("Browser Launched Successfully.");

                LogSuccess("Navigating to University Site https://lms.mercatorum.multiversity.click/");

                await page
                       .GotoAsync("https://lms.mercatorum.multiversity.click/", new PageGotoOptions
                       {
                           WaitUntil = WaitUntilState.NetworkIdle
                       }).ConfigureAwait(true);


                LogWarning("Richiesta Autenticazine..");

                LogRequestInformation("Inserisci il tuo username:"); //ecarollo_0082300299
                Username = GetRiquestInformation();

                LogRequestInformation("Inserisci la tua password:"); //d71602a2
                Password = GetRiquestInformation();

                LogInfo("Inserimento username...");
                var usernameInput = page.Locator("#username").First;
                await usernameInput.ClearAsync();
                await usernameInput.FillAsync(Username);

                LogInfo("Inserimento password...");
                var passwordInput = page.Locator("#password").First;
                await passwordInput.ClearAsync();
                await passwordInput.FillAsync(Password);

                LogInfo("Cliccando sul bottone di login...");
                var loginButton = page.Locator("//button/span[text()='Accedi']").First;
                await loginButton.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                Thread.Sleep(TimeSpan.FromSeconds(4));
                ;
                // Controllo e rimozione walkme
                LogInfo("Verificando presenza di walkme...");

                var walkmeExists = await page.Locator("div[id*='walkme-visual-design']").CountAsync() > 0;

                if (walkmeExists)
                {
                    var popUp= page.Locator("//*[@id='border-49e0cc4f-5895-5af9-52ab-b19efc02d195']").First;
                    await popUp.ClickAsync();
                    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                }
                else
                {
                    LogInfo("Nessun elemento walkme trovato.");
                }

                LogRequestInformation("Inserisci la materia di interesse...");
                Materia = GetRiquestInformation();

                try
                {
                    var DaCompletareButton = page.Locator("//button[text()='Da Completare ']").First;
                    await DaCompletareButton.ClickAsync();
                    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                    Thread.Sleep(TimeSpan.FromSeconds(3));
                }
                catch
                {
                    LogWarning("Bottone 'Da Completare' non trovato.");
                }

                // Cerca la card del corso e naviga all'href del bottone
                LogInfo($"Cercando il corso: {Materia}");

                try
                {//Elaborazione dei segnali e delle informazioni di misura
                    var courseButton = page.Locator($"//span[contains(normalize-space(.),\"{Materia}\")]/ancestor::div[.//a[contains(@href,\"/videolezioni/\")]][1]//a[contains(@href,\"/videolezioni/\")]\r\n ").First;
                    
                    await courseButton.ClickAsync().ConfigureAwait(true);
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                    Thread.Sleep(TimeSpan.FromSeconds(4));
                }
                catch (Exception ex)
                {
                    ;
                }

            }
            catch (Exception ex)
            {
                LogError($"An error occurred: {ex.Message}");
            }
            finally
            {
                if (Browser != null) await Browser.CloseAsync();
                if (Driver != null) Driver.Dispose();

            }

            LogRequestInformation("Press Any Key to Exit...");

            GetRiquestInformation();

        }

        static void LogRequestInformation(string message)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[InsertInformation] {message}");
        }

        static string GetRiquestInformation()
        {
            Console.Write($">");
            string message = Console.ReadLine() ?? string.Empty;
            Console.ResetColor();
            return message;
        }
        static void LogInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[INFO] {message}");
            Console.ResetColor();
        }

        static void LogSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[SUCCESS] {message}");
            Console.ResetColor();
        }

        static void LogError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {message}");
            Console.ResetColor();
        }

        static void LogWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARNING] {message}");
            Console.ResetColor();
        }
    }
}