using BidWinAI.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;
namespace BidWinAI.Services
{
    public class BandoService
    {
        private readonly AppDbContext _dbContext;
        private readonly IWebHostEnvironment _env;
        private readonly OpenAI.OpenAIClient _openAiClient;
        private readonly IConfiguration _config;

        public BandoService(AppDbContext dbContext, IWebHostEnvironment env, OpenAI.OpenAIClient openAiClient, IConfiguration? config = null)
        {
            _dbContext = dbContext;
            _env = env;
            _openAiClient = openAiClient;
            _config = config;
        }

        public async Task<int> SalvaInCodaAsync(IBrowserFile file)
        {
            long maxFileSize = 1024 * 1024 * 15; // 15 MB
            var cartellaDestinazione = Path.Combine(_env.WebRootPath, "bandi_caricati");

            if (!Directory.Exists(cartellaDestinazione))
            {
                Directory.CreateDirectory(cartellaDestinazione);
            }

            var nomeFileUnico = $"{Guid.NewGuid()}_{file.Name}";
            var percorsoFisicoCompleto = Path.Combine(cartellaDestinazione, nomeFileUnico);

            using (var streamInput = file.OpenReadStream(maxFileSize))
            using (var streamOutput = new FileStream(percorsoFisicoCompleto, FileMode.Create))
            {
                await streamInput.CopyToAsync(streamOutput);
            }

            var nuovoBando = new Bando
            {
                NomeFile = file.Name,
                PercorsoFile = $"bandi_caricati/{nomeFileUnico}",
                Dimensione = file.Size,
                DataCaricamento = DateTime.UtcNow,
                UtenteId = 1,
                Stato = StatoBando.InCoda // Parte formalmente in coda
            };

            _dbContext.Bandi.Add(nuovoBando);
            await _dbContext.SaveChangesAsync();

            return nuovoBando.Id; // Restituiamo l'ID per tracciarlo
        }

        public async Task ElaboraBandoEffettivoAsync(int bandoId)
        {
            // 🔍 LOG DI INGRESSO
            Console.WriteLine($"\n[WORKER-ASYNC] 🚀 Ricevuto bando ID {bandoId}. Inizio elaborazione in background...");

            // Recuperiamo il bando (usiamo un dbContext fresco passato dal worker)
            var bando = await _dbContext.Bandi.FindAsync(bandoId);
            if (bando == null)
            {
                Console.WriteLine($"[WORKER-ASYNC] ❌ ERRORE: Impossibile trovare il bando con ID {bandoId} nel database.");
                return;
            }

            try
            {
                // 1. Aggiorna lo stato in lavorazione
                Console.WriteLine($"[WORKER-ASYNC] ⚙️ 1. Aggiornamento stato bando in 'InElaborazione' su Postgres...");
                bando.Stato = StatoBando.InElaborazione;
                await _dbContext.SaveChangesAsync();

                var percorsoFisicoCompleto = Path.Combine(_env.WebRootPath, bando.PercorsoFile);
                Console.WriteLine($"[WORKER-ASYNC] 📂 File da elaborare posizionato in: {percorsoFisicoCompleto}");

                // 2. Estrazione testo
                Console.WriteLine("[WORKER-ASYNC] 📄 2. Avvio estrazione testo dal PDF (PdfPig)...");
                string testoEstratto = EstraiTestoDaPdf(percorsoFisicoCompleto);
                bando.TestoEstratto = testoEstratto;
                Console.WriteLine($"[WORKER-ASYNC] ✅ Estrazione completata! Caratteri estratti: {testoEstratto.Length}");

                // 3. AI Crunching
                Console.WriteLine("[WORKER-ASYNC] 🧠 3. Preparazione prompt e configurazione client OpenRouter...");
                ChatClient chatClient = _openAiClient.GetChatClient($"{_config["AI_MODEL"]}");

                string prompt = "Sei un assistente esperto di gare d'appalto. Analizza il testo del seguente bando ed estrai in modo schematico:\n1. Oggetto\n2. Importo\n3. Requisiti\n4. Scadenza\n\n" + $"Testo:\n{testoEstratto}";
                var messaggi = new ChatMessage[] { ChatMessage.CreateUserMessage(prompt) };

                ChatCompletionOptions opzioni = new ChatCompletionOptions() { MaxOutputTokenCount = 2000 };

                Console.WriteLine("[WORKER-ASYNC] 🌐 Invio richiesta a Gemini (OpenRouter)... ATTESA RISPOSTA DELL'IA...");
                ChatCompletion completion = await chatClient.CompleteChatAsync(messaggi, opzioni);

                bando.AnalisiIA = completion.Content[0].Text;
                bando.Stato = StatoBando.Completato;

                Console.WriteLine("[WORKER-ASYNC] 🎉 Risposta ricevuta con successo! Stato bando impostato su 'Completato'.");
            }
            catch (Exception ex)
            {
                bando.Stato = StatoBando.Fallito;
                bando.MessaggioErrore = ex.Message;

                Console.WriteLine($"[WORKER-ASYNC] ❌ INTERRUZIONE PER ERRORE: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("[WORKER-ASYNC] 💾 Salvataggio finale dello stato e dei testi su PostgreSQL...");
                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"[WORKER-ASYNC] 🏁 Fine ciclo per bando ID {bandoId}.\n");
            }
        }
        private string EstraiTestoDaPdf(string percorsoFile)
        {
            var testo = new System.Text.StringBuilder();
            using (var document = UglyToad.PdfPig.PdfDocument.Open(percorsoFile))
            {
                foreach (var page in document.GetPages())
                {
                    testo.AppendLine(page.Text);
                }
            }
            return testo.ToString();
        }

        public async Task RispondiInChatAsync(int bandoId, string testoBando, string testoDomanda)
        {
            // 1. Salva subito la domanda dell'utente sul DB (Usando MessaggiChatAi)
            var msgUtente = new MessaggiChatAi { BandoId = bandoId, Testo = testoDomanda, IsAi = false };
            _dbContext.MessaggiChat.Add(msgUtente);
            await _dbContext.SaveChangesAsync();

            // 2. Recupera la cronologia passata per darla all'AI
            var cronologiaDB = await _dbContext.MessaggiChat.Where(m => m.BandoId == bandoId).OrderBy(m => m.DataCreazione).ToListAsync<MessaggiChatAi>();

            var messaggiSDK = new List<ChatMessage> {
        ChatMessage.CreateSystemMessage($"Rispondi basandoti sul bando:\n{testoBando}")
    };

            foreach (var m in cronologiaDB)
            {
                messaggiSDK.Add(m.IsAi ? ChatMessage.CreateAssistantMessage(m.Testo) : ChatMessage.CreateUserMessage(m.Testo));
            }

            // 3. Chiama l'AI
            string nomeModello = Environment.GetEnvironmentVariable("AI_MODEL") ?? "google/gemma-2-9b-it:free";
            ChatClient chatClient = _openAiClient.GetChatClient(nomeModello);

            ChatCompletion completion = await chatClient.CompleteChatAsync(messaggiSDK);
            string rispostaAI = completion.Content[0].Text;

            // 4. Salva la risposta dell'AI sul DB (Usando MessaggiChatAi)
            var msgAi = new MessaggiChatAi { BandoId = bandoId, Testo = rispostaAI, IsAi = true };
            _dbContext.MessaggiChat.Add(msgAi);
            await _dbContext.SaveChangesAsync();
        }
    }
}