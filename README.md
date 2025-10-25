# UniversityUtility
# University Telegram Bot

Bot Telegram per automatizzare la visualizzazione delle video lezioni universitarie con controllo remoto.

---

## Setup Completo (5 minuti)

### 1. Crea il Bot su Telegram

1. Apri Telegram e cerca **@BotFather**
2. Invia il comando `/newbot`
3. Scegli un nome (es. "UniMerc Video Bot")
4. Scegli un username (es. "UnimercVideo_bot")
5. **COPIA IL TOKEN** che ti viene fornito (formato: `1234567890:ABCdefGHI...`)

### 2. Ottieni il tuo Chat ID

1. Cerca **@userinfobot** su Telegram
2. Invia `/start`
3. **COPIA IL NUMERO** che ti viene mostrato (es. `5063487515`)
   - ATTENZIONE: Questo e il TUO Chat ID, NON l'ID del bot

### 3. Configura il file config.json

Apri `UniversityUtility.TelegramBot/config.json` e inserisci i tuoi dati:

```json
{
  "TelegramBot": {
    "BotToken": "INCOLLA_QUI_IL_TOKEN_DEL_BOT",
    "ChatId": INCOLLA_QUI_IL_CHAT_ID
  },
  "University": {
    "Username": "tuo_username_universita",
    "Password": "tua_password",
    "DefaultSubject": "Nome della Materia",
    "SaveCredentials": true
  }
}
```

### 4. Avvia la conversazione con il bot

**IMPORTANTE:** Prima di avviare l'applicazione, devi iniziare la conversazione

1. Apri Telegram
2. Cerca il tuo bot (es. @UnimercVideo_bot)
3. Clicca sul pulsante **START** (o invia /start)
4. Riceverai un messaggio di benvenuto con tutti i comandi

### 5. Avvia l'applicazione

```bash
cd UniversityUtility.TelegramBot
dotnet run
```

Dovresti vedere:
```
Bot @UnimercVideo_bot connesso con successo
ID Bot: 8466641137
Chat ID autorizzata: 5063487515
Bot pronto per ricevere comandi
Messaggio di benvenuto inviato su Telegram
```

---

## Comandi Disponibili

### /start - Messaggio di benvenuto

Mostra il messaggio di benvenuto con tutti i comandi disponibili e le credenziali configurate.

```
/start
```

### /run - Avvia automazione

**Con credenziali salvate in config.json:**
```
/run
```
Usa le credenziali e la materia configurate.

**Con materia specifica:**
```
/run Elaborazione dei segnali
```
Usa le credenziali salvate ma cambia materia.

**Con credenziali custom:**
```
/run mio_username mia_password Nome Materia
```
Specifica tutto manualmente.

**Esempi:**
```
/run
/run Sicurezza delle reti e Cyber Security
/run ecarollo_0082300299 d71602a2 Elaborazione dei segnali
```

### /status - Stato automazione

Mostra lo stato corrente:
- Se c'e un'automazione attiva
- Quale materia sta processando
- Quale lezione e in corso
- Progresso dell'automazione

```
/status
```

### /screenshot - Screenshot della pagina

Ricevi uno screenshot istantaneo della pagina del browser.
Utile per vedere cosa sta facendo il bot in tempo reale.

```
/screenshot
```

### /stop - Ferma automazione

Ferma l'automazione in corso e chiude il browser.

```
/stop
```

---

## Esempio di Utilizzo

1. **Avvia il bot** sul tuo PC:
   ```bash
   dotnet run
   ```

2. **Su Telegram**, invia per prima cosa:
   ```
   /start
   ```
   Riceverai:
   ```
   Bot Universita Avviato

   Comandi disponibili:

   /start - Mostra questo messaggio
   /run - Avvia automazione
   /status - Stato automazione
   /screenshot - Screenshot pagina
   /stop - Ferma automazione

   Credenziali salvate
   Username: ecarollo_0082300299
   Materia predefinita: Sicurezza delle reti e Cyber Security

Usa /run per avviare con le credenziali salvate
   oppure /run <materia> per cambiare materia
   oppure /run <username> <password> <materia> per credenziali custom
   ```

3. **Avvia l'automazione:**
   ```
   /run
   ```
   Riceverai:
   ```
   Automazione avviata

   Utente: ecarollo_0082300299
   Materia: Sicurezza delle reti e Cyber Security
   ```

4. **Riceverai notifiche in tempo reale:**
   ```
   Materia selezionata: Sicurezza delle reti e Cyber Security
   Inizio visualizzazione video lezioni...
   Trovate 15 lezioni totali
   
   Inizio Lezione 1/15
   Lezione 1/15
 [#####-----] 25.0%
   
   Lezione 1/15
   [##########] 100.0%
   Completata Lezione 1/15
   ```

5. **Controlla lo stato** in qualsiasi momento:
   ```
   /status
 ```

6. **Vuoi vedere cosa sta facendo?**
   ```
   /screenshot
   ```

7. **Ferma tutto:**
   ```
   /stop
   ```

---

## Risoluzione Problemi

### Errore: "Bad Request: chat not found"

**Causa:** Non hai ancora avviato la conversazione con il bot su Telegram.

**Soluzione:**
1. Apri Telegram
2. Cerca il tuo bot (es. @UnimercVideo_bot)
3. Clicca START
4. Riavvia l'applicazione con `dotnet run`

### Errore: "Unauthorized" o "401"

**Causa:** Il Bot Token non e valido o e stato revocato.

**Soluzione:**
1. Apri Telegram e cerca @BotFather
2. Invia /token e seleziona il tuo bot
3. Copia il nuovo token
4. Aggiorna config.json con il token corretto

### Errore: "Exception during making request"

**Causa:** Problemi di connessione alla rete o token malformato.

**Soluzione:**
- Verifica la connessione Internet
- Controlla che il token sia nel formato corretto: 1234567890:ABCdefGHI...
- Verifica che il firewall non blocchi la connessione a Telegram
- Se necessario, usa una VPN

### Il bot non risponde ai comandi

**Verifica che:**
1. L'applicazione sia in esecuzione (dotnet run)
2. Hai fatto /start al bot su Telegram
3. Il Chat ID in config.json sia corretto
4. Il token sia valido

### Il browser non si apre

**Soluzione:**
1. Assicurati di avere Microsoft Edge installato
2. Installa Playwright con:
   ```bash
   pwsh bin/Debug/net9.0/playwright.ps1 install
   ```

---

## Sicurezza

### IMPORTANTE

- NON condividere il token del bot con nessuno
- NON committare il file config.json su Git (e gia nel .gitignore)
- Il bot accetta comandi SOLO dal Chat ID configurato
- Le credenziali vengono passate tramite comandi (usa con cautela in chat di gruppo)

### Token esposto su Git?

Se hai accidentalmente committato il token:

1. **Revoca immediatamente il token:**
   - Apri @BotFather su Telegram
   - Invia /revoke
   - Seleziona il bot
   - Conferma la revoca

2. **Genera un nuovo token:**
   - Invia /token a @BotFather
   - Seleziona il bot
   - Copia il nuovo token
   - Aggiorna config.json

3. **Rimuovi il file dalla cronologia Git:**
   ```bash
   git rm --cached UniversityUtility.TelegramBot/config.json
   git commit -m "Remove exposed config"
   ```

---

## Funzionalita

- Controllo remoto completo - Controlla l'automazione da smartphone
- Notifiche in tempo reale - Ricevi aggiornamenti su ogni lezione
- Screenshot on-demand - Vedi cosa sta facendo il bot
- Sicuro - Risponde solo al tuo Chat ID
- Veloce - Comandi istantanei via Telegram
- Credenziali salvate - Configurale una volta sola

---

## Struttura Progetti

- **UniversityUtility** - Applicazione console originale (input manuale)
- **UniversityUtility.Core** - Logica di automazione condivisa
- **UniversityUtility.TelegramBot** - Bot Telegram per controllo remoto

---

## FAQ

**Q: Posso usare il bot da piu dispositivi?**  
A: Si. Una volta configurato, puoi inviare comandi da qualsiasi dispositivo Telegram con il tuo account.

**Q: Il bot continua a funzionare se chiudo Telegram?**  
A: Si, ma devi tenere aperta l'applicazione sul PC con dotnet run.

**Q: Posso automatizzare piu materie contemporaneamente?**  
A: No, per ora il bot supporta una materia alla volta. Usa /stop e poi /run <altra_materia>.

**Q: Le credenziali sono sicure?**  
A: Le credenziali sono salvate in locale nel file config.json. Assicurati che non venga committato su Git pubblici.

**Q: Posso schedulare l'automazione a orari specifici?**  
A: Non ancora, ma e una funzionalita pianificata per il futuro.

**Q: Qual e la differenza tra /start e /run?**  
A: /start mostra solo il messaggio di benvenuto con i comandi disponibili. /run avvia effettivamente l'automazione delle lezioni.

---

## Supporto

Se hai problemi:

1. Verifica di aver seguito tutti i passaggi del setup
2. Controlla i messaggi di errore sulla console
3. Verifica che il bot sia stato avviato su Telegram con /start
4. Controlla che config.json sia configurato correttamente

---

**Fatto! Ora puoi controllare le tue lezioni universitarie da ovunque con Telegram!**
