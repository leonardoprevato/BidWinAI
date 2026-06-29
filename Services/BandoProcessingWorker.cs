using BidWinAI.Models;
using Microsoft.EntityFrameworkCore;

namespace BidWinAI.Services
{
    public class BandoProcessingWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BandoProcessingWorker> _logger;

        public BandoProcessingWorker(IServiceScopeFactory scopeFactory, ILogger<BandoProcessingWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🤖 BandoProcessingWorker avviato e pronto in background.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Creiamo uno scope isolato perché i DbContext in background non possono essere singleton
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
                        var openAiClient = scope.ServiceProvider.GetRequiredService<OpenAI.OpenAIClient>();


                        // Crea un'istanza locale di BandoService collegata a questo scope
                        var bandoService = new BandoService(dbContext, env, openAiClient);

                        // Cerca il primo bando che attende di essere elaborato
                        var bandoInCoda = await dbContext.Bandi
                            .FirstOrDefaultAsync(b => b.Stato == StatoBando.InCoda, stoppingToken);

                        if (bandoInCoda != null)
                        {
                            _logger.LogInformation($"🤖 [WORKER] Trovato bando da elaborare: ID {bandoInCoda.Id}");

                            // Avvia l'elaborazione pesante
                            await bandoService.ElaboraBandoEffettivoAsync(bandoInCoda.Id);

                            _logger.LogInformation($"🤖 [WORKER] Elaborazione completata per bando ID {bandoInCoda.Id}");
                        }
                    }
                    using (var scope = _scopeFactory.CreateScope())
                    {

                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ [WORKER] Errore nel ciclo di background: {ex.Message}");
                }

                // Aspetta 5 secondi prima di controllare nuovamente il database
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}