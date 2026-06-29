namespace BidWinAI.Models
{
    public class MessaggiChatAi
    {
        public int Id { get; set; }
        public int BandoId { get; set; }
        public Bando Bando { get; set; }

        public string Testo { get; set; }
        public bool IsAi { get; set; } // true = risposta IA, false = domanda utente
        public DateTime DataCreazione { get; set; } = DateTime.UtcNow;

        // Questo flag serve al Worker per capire cosa deve elaborare!
        public bool Elaborato { get; set; } = false;
    }
}
