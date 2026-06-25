using Microsoft.EntityFrameworkCore;

namespace BidWinAI.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Bando> Bandi { get; set; }
    public DbSet<Utente> Utente { get; set; }
    public string? TestoEstratto { get; set; } // Contiene tutto il testo del PDF
    public string? AnalisiIA { get; set; }     // Contiene il riassunto/analisi generato dall'IA
}