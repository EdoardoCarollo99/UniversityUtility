@echo off
echo ================================
echo   UNIVERSITY TELEGRAM BOT
echo ================================
echo.

REM Controlla se il file di configurazione esiste
if not exist "UniversityUtility.TelegramBot\config.json" (
echo [ERRORE] File config.json non trovato!
  echo.
    echo Crea il file 'UniversityUtility.TelegramBot\config.json'
    echo.
    echo Usa 'config.json.example' come template:
    echo   1. Copia config.json.example in config.json
  echo   2. Modifica i valori con le tue credenziali
    echo.
    echo Esempio:
    echo {
    echo   "TelegramBot": {
    echo     "BotToken": "il_tuo_token_da_@BotFather",
    echo     "ChatId": 123456789
    echo   }
    echo }
    echo.
pause
    exit /b 1
)

echo [OK] File di configurazione trovato
echo.
echo Avvio bot...
echo.

cd UniversityUtility.TelegramBot
dotnet run

pause
