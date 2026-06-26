using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using BidWinAI.Models;
using System.IO;
using OpenAI.Chat;

namespace BidWinAI.Services
{
    public class BandoService
    {
        private readonly AppDbContext _dbContext;
        private readonly IWebHostEnvironment _env;
        private readonly OpenAI.OpenAIClient _openAiClient;

        public BandoService(AppDbContext dbContext, IWebHostEnvironment env, OpenAI.OpenAIClient openAiClient)
        {
            _dbContext = dbContext;
            _env = env;
            _openAiClient = openAiClient;
        }

        // 🟢 FASE 1: Chiamata dalla UI Blazor - Operazione istantanea
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

        // 🟢 FASE 2: Chiamata dal Worker in Background - Elaborazione pesante
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
                ChatClient chatClient = _openAiClient.GetChatClient("google/gemma-4-31b-it:free");

                string prompt = "Sei un assistente esperto di gare d'appalto. Analizza il testo del seguente bando ed estrai in modo schematico:\n1. Oggetto\n2. Importo\n3. Requisiti\n4. Scadenza\n\n" + $"Testo:\n{testoEstratto}";
                var messaggi = new ChatMessage[] { ChatMessage.CreateUserMessage(prompt) };

                ChatCompletionOptions opzioni = new ChatCompletionOptions() { MaxOutputTokenCount = 2000 };

                Console.WriteLine("[WORKER-ASYNC] 🌐 Invio richiesta a Gemini (OpenRouter)... ATTESA RISPOSTA DELL'IA...");
                ChatCompletion completion = await chatClient.CompleteChatAsync(messaggi, opzioni);

                bando.AnalisiIA = completion.Content[0].Text;
                bando.Stato = StatoBando.Completato; // Ce l'abbiamo fatta!

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
    }
}