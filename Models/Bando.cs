namespace BidWinAI.Models;

public class Bando
{
    public int Id { get; set; }
    public string NomeFile { get; set; } = string.Empty;
    public string PercorsoFile { get; set; } = string.Empty;
    public long Dimensione { get; set; }
    public DateTime DataCaricamento { get; set; } = DateTime.UtcNow;

    // Chiave Esterna (Foreign Key) verso l'utente
    public int UtenteId { get; set; }

    // Proprietà di navigazione inversa
    public Utente? Utente { get; set; }
    public string? TestoEstratto { get; set; }
    public string? AnalisiIA { get; set; }
}


