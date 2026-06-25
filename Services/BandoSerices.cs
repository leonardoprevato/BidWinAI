using BidWinAI.Models;
using Microsoft.AspNetCore.Components.Forms;
using OpenAI;
using OpenAI.Chat;
using UglyToad.PdfPig;
using System.Text;

namespace BidWinAI.Services;

public class BandoService
{
    private readonly AppDbContext _dbContext;
    private readonly OpenAIClient _openAiClient;
    private readonly IWebHostEnvironment _env;

    // Il framework inietterà automaticamente questi 3 oggetti configurati nel Program.cs
    public BandoService(AppDbContext dbContext, OpenAIClient openAiClient, IWebHostEnvironment env)
    {
        _dbContext = dbContext;
        _openAiClient = openAiClient;
        _env = env;
    }

    public async Task ElaboraESalvaBandoAsync(IBrowserFile file)
    {
        long maxFileSize = 1024 * 1024 * 15; // 15 MB
        var cartellaDestinazione = Path.Combine(_env.WebRootPath, "bandi_caricati");

        if (!Directory.Exists(cartellaDestinazione))
        {
            Directory.CreateDirectory(cartellaDestinazione);
        }

        // 1. GENERAZIONE NOME UNICO E SALVATAGGIO SU HARD DISK
        var nomeFileUnico = $"{Guid.NewGuid()}_{file.Name}";
        var percorsoFisicoCompleto = Path.Combine(cartellaDestinazione, nomeFileUnico);

        using (var streamInput = file.OpenReadStream(maxFileSize))
        using (var streamOutput = new FileStream(percorsoFisicoCompleto, FileMode.Create))
        {
            await streamInput.CopyToAsync(streamOutput);
        }

        var percorsoRelativo = $"bandi_caricati/{nomeFileUnico}";

        // 2. ESTRAZIONE DEL TESTO DAL PDF (PdfPig)
        string testoEstratto = EstraiTestoDaPdf(percorsoFisicoCompleto);

        // 3. CHIAMATA A OPENROUTER (Tramite il client registrato nel Program.cs)
        // Puoi cambiare il modello inserendo quello che preferisci di OpenRouter (es. "google/gemini-2.5-flash" o "openai/gpt-4o")
        ChatClient chatClient = _openAiClient.GetChatClient("google/gemini-2.5-flash");

        string prompt =
            "Sei un assistente esperto di gare d'appalto. Analizza il testo del seguente bando ed estrai in modo schematico:\n" +
            "1. Oggetto dell'appalto\n" +
            "2. Importo totale stimato\n" +
            "3. Requisiti principali\n" +
            "4. Data di scadenza\n\n" +
            $"Testo bando:\n{testoEstratto}";

        string analisiIA = "";
        try
        {
            ChatCompletion completion = await chatClient.CompleteChatAsync(prompt);
            analisiIA = completion.Content[0].Text;
        }
        catch (Exception ex)
        {
            analisiIA = $"❌ Errore OpenRouter: {ex.Message}";
        }

        // 4. SALVATAGGIO FINALE SU POSTGRESQL TRAMITE DBCONTEXT
        var nuovoBando = new Bando
        {
            NomeFile = file.Name,
            PercorsoFile = percorsoRelativo,
            Dimensione = file.Size,
            DataCaricamento = DateTime.UtcNow,
            UtenteId = 1, // Assicurati sempre che l'utente 1 esista nel DB
            TestoEstratto = testoEstratto,
            AnalisiIA = analisiIA
        };

        _dbContext.Bandi.Add(nuovoBando);
        await _dbContext.SaveChangesAsync();
    }

    private string EstraiTestoDaPdf(string percorsoFile)
    {
        var sb = new StringBuilder();
        try
        {
            using (var pdf = PdfDocument.Open(percorsoFile))
            {
                foreach (var pagina in pdf.GetPages())
                {
                    sb.AppendLine(pagina.Text);
                }
            }
            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"[Errore estrazione PDF: {ex.Message}]";
        }
    }
}