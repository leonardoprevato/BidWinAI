namespace BidWinAI.Models;

public class Utente
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Relazione One-to-Many: Un utente ha una collezione di Bandi
    public List<Bando> Bandi { get; set; } = new();
}