namespace BidWinAI.Models
{
    // 🟢 NUOVO: Definiamo gli stati possibili del bando
    public enum StatoBando
    {
        InCoda,
        InElaborazione,
        Completato,
        Fallito
    }

    public class Bando
    {
        public int Id { get; set; }
        public string NomeFile { get; set; } = string.Empty;
        public string PercorsoFile { get; set; } = string.Empty;
        public long Dimensione { get; set; }
        public DateTime DataCaricamento { get; set; }
        public int UtenteId { get; set; }
        public Utente? Utente { get; set; }

        public string TestoEstratto { get; set; } = string.Empty;
        public string AnalisiIA { get; set; } = string.Empty;

        // 🟢 NUOVE COLONNE PER L'APPROCCIO PROFESSIONALE
        public StatoBando Stato { get; set; } = StatoBando.InCoda;
        public string? MessaggioErrore { get; set; }
    }
}