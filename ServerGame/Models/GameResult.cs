namespace ServerGame.Models
{
    public class GameResult
    {
        public int Id { get; set; }
        public string PlayerName { get; set; }
        public DateTime Date { get; set; }
        public string Result { get; set; } // "X" или "O"
    }
}
