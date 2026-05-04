namespace Api.Entities;

public class MatchHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TournamentCode { get; set; }
    public string GameType { get; set; }
    
    public Guid PlayerOneId { get; set; }
    public string PlayerOneUsername { get; set; }
    public Guid PlayerTwoId { get; set; }
    public string PlayerTwoUsername { get; set; }

    public Guid? WinnerId { get; set; } // Null if Draw
    public string MatchStatus { get; set; } = "Finished"; // "Finished", "Draw", "Aborted"
    
    public List<Move> Moves { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public MatchHistory() { }

    public MatchHistory(string tournamentCode, string gameType)
    {
        TournamentCode = tournamentCode;
        GameType = gameType;
    }

    public MatchHistory(string tournamentCode, string gameType, Guid p1Id, string p1Name, Guid p2Id, string p2Name)
    {
        TournamentCode = tournamentCode;
        GameType = gameType;
        PlayerOneId = p1Id;
        PlayerOneUsername = p1Name;
        PlayerTwoId = p2Id;
        PlayerTwoUsername = p2Name;
        Moves = new List<Move>();
    }
}

public class Move
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty; 
    public string MovesJson { get; set; } = "[]";
    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
}