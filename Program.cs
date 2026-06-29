using System.ClientModel;
using BidWinAI.Components;
using BidWinAI.Models;
using BidWinAI.Services;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using OpenAI;

// 1. CARICA IL FILE .ENV PRIMA DI TUTTO
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Carica le variabili d'ambiente nel sistema di configurazione di .NET
builder.Configuration.AddEnvironmentVariables();

// 2. REGISTRAZIONE DEI SERVIZI (Una sola volta ciascuno!)
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddHostedService<BandoProcessingWorker>();
builder.Services.AddScoped<BandoService>();
// 3. RECUPERO DELLE VARIABILI DAL .ENV E COMPOSIZIONE STRINGA DI CONNESSIONE
// Questo evita l'errore del '%DB_PORT%' prendendo i valori reali puliti
var dbHost = builder.Configuration["DB_HOST"];
var dbPort = builder.Configuration["DB_PORT"];
var dbDatabase = builder.Configuration["DB_DATABASE"];
var dbUser = builder.Configuration["DB_USERNAME"];
var dbPass = builder.Configuration["DB_PASSWORD"];

var connectionString = $"Host={dbHost};Port={dbPort};Database={dbDatabase};Username={dbUser};Password={dbPass}";

// Registra la Factory per PostgreSQL (Risolve l'errore in Archivio.razor)
builder.Services.AddPooledDbContextFactory<AppDbContext>(options => options.UseNpgsql(connectionString));

// OPZIONALE: Se altre parti del codice (es. BandoService) richiedono 
// AppDbContext direttamente nel costruttore, registra anche il context standard 
// dicendogli di usare la stessa factory per generarsi.
builder.Services.AddScoped(p => p.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

// 4. CONFIGURAZIONE SINGLETON PER OPENROUTER (La tua logica corretta)
builder.Services.AddSingleton(sp =>
{
    var options = new OpenAIClientOptions
    {
        Endpoint = new Uri("https://openrouter.ai/api/v1")
    };

    var openRouterApiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? "";
    var credential = new ApiKeyCredential(openRouterApiKey);

    return new OpenAIClient(credential, options);
});

var app = builder.Build();

// 5. CONFIGURAZIONE DELLA PIPELINE DI RICHIESTA (MIDDLEWARE)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();