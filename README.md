# BidWinAI - Sistema di Gestione Documentale con Integrazione AI

BidWinAI è una piattaforma web aziendale sviluppata in ambiente .NET per automatizzare l'analisi, l'estrazione dati e la sintesi di documenti complessi come bandi di gara, capitolati tecnici e profili professionali. Il sistema integra modelli di intelligenza artificiale direttamente nel flusso di lavoro gestionale attraverso un'architettura asincrona.

---

## Flusso di lavoro e finalità d'uso

1. **Caricamento**
L'utente trascina o seleziona il file PDF nell'area di upload dell'interfaccia web.

2. **Presa in carico**
Il sistema salva il documento sul server e registra la richiesta nel database PostgreSQL. L'interfaccia utente si sblocca immediatamente, consentendo di proseguire il lavoro sul gestionale mentre il processo avanza in background.

3. **Elaborazione**
Un servizio dedicato (libreria .NET) in background estrae il testo dal file PDF e interroga l'intelligenza artificiale tramite API per mappare i dati rilevanti.

4. **Consultazione**
Al termine del processo, la dashboard mostra una scheda riepilogativa che organizza le informazioni estratte: oggetto del documento, importi economici, requisiti tecnici obbligatori, scadenze temporali e un riassunto esecutivo del testo, l'AI è anche in grado di riconoscere se è un bando di gara ufficiale.

---

## Architettura tecnica

Per mantenere l'applicazione fluida e reattiva, il progetto implementa il pattern **Database-Driven e Background Worker**. Frontend e backend sono disaccoppiati e utilizzano il database come coordinatore degli stati dei file.

* **Frontend Blazor**: Gestisce le viste utente e aggiorna l'interfaccia monitorando lo stato dei documenti sul database.
* **Background Worker**: Un servizio nativo di .NET (classe derivata da `BackgroundService`) che esegue un ciclo continuo su un thread dedicato, isolando i carichi di lavoro della CPU e le chiamate di rete esterne.
* **Gestione degli Scope**: Il worker utilizza `IServiceScopeFactory` per isolare il contesto di database (`DbContext`) a ogni ciclo di controllo. Questo approccio evita connessioni persistenti corrotte e garantisce un corretto rilascio della memoria RAM.
* **Gestione dei Timeout**: Le chiamate verso i provider AI esterni sono regolate tramite `CancellationToken`.

---

## Stack tecnologico

* **Framework**: .NET 10.0.301 / ASP.NET Core (Blazor Web App)
* **Linguaggio**: C#
* **Database**: PostgreSQL
* **ORM**: Entity Framework Core
* **Estrazione testo**: PdfPig (parsing di PDF nativi)
* **Integrazione AI**: OpenRouter API
* **Interfaccia grafica**: Tailwind CSS

---

## Gestione degli stati del documento

Il ciclo di vita di ogni file è tracciato rigidamente sul database per garantire la resilienza del sistema anche in caso di riavvio improvviso del server:

| Stato | Descrizione |
| :--- | :--- |
| **InCoda** | Il file è stato caricato ed è memorizzato sul server, in attesa di essere prelevato dal servizio in background. |
| **InElaborazione** | Il worker ha preso in carico il file, sta eseguendo il parsing del testo e attende la risposta dall'intelligenza artificiale. |
| **Completato** | L'analisi è terminata con successo. I dati estratti e la sintesi sono pronti sul database per essere consultati. |
| **Fallito** | Si è verificato un errore critico (timeout, file corrotto o credenziali API non valide). Il motivo del blocco viene registrato nei log. |

---

## Installazione e configurazione locale

### Prerequisiti
* .NET 9 SDK
* Istanza attiva di PostgreSQL
* Node.js (necessario per il compilatore delle classi di Tailwind CSS)

### Procedura di avvio

1. Clonare il repository:
```bash
git clone [https://github.com/tuo-username/BidWinAI.git](https://github.com/tuo-username/BidWinAI.git)
cd BidWinAI
