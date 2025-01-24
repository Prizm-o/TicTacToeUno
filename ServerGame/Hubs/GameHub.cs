using Microsoft.AspNetCore.SignalR;
using ServerGame.Models;

namespace ServerGame.Hubs
{
    public class GameHub : Hub
    {
        private static List<string> connectedUsers = new List<string>();
        private static string[] Board = new string[9];
        private static List<User> players = new List<User> ();
        private static string currentPlayer = "X";

        public class User
        {
            public string Id;
            public string Name;
            public string Value;

            public User(string id, string name, string value)
            {
                Id = id;
                Name = name;
                Value = value;
            }
        }

        public async Task Connect(string playerName, string userId)
        {
            connectedUsers.Add(userId);
            if (players.Count < 2)
            {
                Clients.Caller.ToString();
                connectedUsers.Add(userId);
                players.Add(new User(userId, playerName, ""));
                await Clients.Caller.SendAsync("ReceiveMessage", "Вы подключены к игре!");
                if (players.Count == 1)
                {
                    await Clients.Caller.SendAsync("ReceiveMessage", "Ожидание ещё 1 игрока");
                }
                if (players.Count == 2)
                {
                    players[0].Value = "X";
                    players[1].Value = "O";
                    await Clients.Clients(connectedUsers).SendAsync("ReceiveMessage", $"{players[0].Name} играет за {players[0].Value} : {players[1].Name} играет за {players[1].Value}");
                    await Clients.Clients(connectedUsers).SendAsync("UpdateScore", LoadScore());
                    //await Clients.All.SendAsync("PlayerStatus", playerName + " играет за O", "O");
                    await Clients.Client(connectedUsers[0]).SendAsync("StartGame");
                }
            }
            else
            {
                await Clients.Caller.SendAsync("ReceiveMessage", $"Игра уже началась, вы можете наблюдать. {players[0].Name} играет за {players[0].Value} : {players[1].Name} играет за {players[1].Value}");
                await Clients.Caller.SendAsync("UpdateBoardViewers", Board);
                await Clients.Clients(connectedUsers).SendAsync("UpdateScore", LoadScore());
            }
            await Clients.Caller.SendAsync("JoinGame");
        }

        public async Task Disconnect(string userId)
        {
            if (players.Find(r => r.Id == userId) != null)
            {
                string playerName = players.Find(r => r.Id == userId).Name;
                players.RemoveAll(r => r.Id == userId);
                await Clients.Clients(connectedUsers).SendAsync("GameOver", $"{playerName} вышел из игры");
                await Clients.Clients(connectedUsers).SendAsync("ExitGame");
                connectedUsers.Clear();
                players.Clear();
                Board = new string[9];
            }
            else
            {
                await Clients.Caller.SendAsync("GameOver", "Вы вышли из игры");
                await Clients.Caller.SendAsync("ExitGame");
                connectedUsers.Remove(userId);
            }            
        }

        private string LoadScore()
        {
            string score;
            using (GameContext db = new GameContext())
            {
                var results = db.GameResults.ToList();
                score = $"{players[0].Name}-побед: " + results.Where(r => r.PlayerName == players[0].Name && r.Result == "Win").Count().ToString() + @" | " + $"{players[1].Name}-побед: " + results.Where(r => r.PlayerName == players[1].Name && r.Result == "Win").Count().ToString();
            }
            return score;
        }

        public async Task MakeMove(int x, string userId)
        {
            int index = x; // Преобразуем 2D координаты в 1D индекс

            // Логика для изменения состояния доски
            currentPlayer = players.Find(r=>r.Id==userId).Value;
            string playerName = players.Find(r => r.Id == userId).Name;
            string opponentName = players.Find(r => r.Id != userId).Name;
            Board[index] = currentPlayer == "X" ? "X" : "O"; // Например, 1 для игрока
            
            if (CheckWin())
            {
                await Clients.Clients(connectedUsers).SendAsync("GameOver", $"{playerName} победил!");

                GameResult playerResults = new GameResult
                {
                    PlayerName = playerName,
                    Date = DateTime.UtcNow,
                    Result = "Win"
                };
                GameResult opponentResults = new GameResult
                {
                    PlayerName = opponentName,
                    Date = DateTime.UtcNow,
                    Result = "Lose"
                };

                using (GameContext db = new GameContext())
                {
                    db.GameResults.Add(playerResults);
                    db.GameResults.Add(opponentResults);
                    db.SaveChanges();
                }

                await Clients.Clients(connectedUsers).SendAsync("UpdateScore", LoadScore());
                await Clients.Clients(connectedUsers).SendAsync("ExitGame");

                connectedUsers.Clear();
                players.Clear();
                Board = new string[9];
            }
            else if (IsBoardFull())
            {
                await Clients.Clients(connectedUsers).SendAsync("GameOver", "Ничья!");
                await Clients.Clients(connectedUsers).SendAsync("ExitGame");
                connectedUsers.Clear();
                players.Clear();
                Board = new string[9];                
            }
            else
            {
                // Уведомляем всех клиентов об изменении состояния игры
                await Clients.Clients(connectedUsers).SendAsync("UpdateBoardViewers", Board);
                await Clients.Clients(new List<string>() { players[0].Id, players[1].Id }).SendAsync("UpdateBoard", Board);
                await Clients.Caller.SendAsync("NextMove");
            }
        }

        private bool CheckWin()
        {
            // Проверка строк, столбцов и диагоналей на победу
            if ((Board[0] == currentPlayer && Board[4] == currentPlayer && Board[8] == currentPlayer) ||
                (Board[2] == currentPlayer && Board[4] == currentPlayer && Board[6] == currentPlayer) ||
                (Board[0] == currentPlayer && Board[1] == currentPlayer && Board[2] == currentPlayer) ||
                (Board[3] == currentPlayer && Board[4] == currentPlayer && Board[5] == currentPlayer) ||
                (Board[6] == currentPlayer && Board[7] == currentPlayer && Board[8] == currentPlayer) ||
                (Board[0] == currentPlayer && Board[3] == currentPlayer && Board[6] == currentPlayer) ||
                (Board[1] == currentPlayer && Board[4] == currentPlayer && Board[7] == currentPlayer) ||
                (Board[2] == currentPlayer && Board[5] == currentPlayer && Board[8] == currentPlayer))
            { 
                return true; 
            }
            return false;
        }

        private bool IsBoardFull()
        {
            foreach (var cell in Board)
            {
                if (cell == null) return false;
            }
            return true;
        }

        public async Task Send(string user)
        {
            await Clients.All.SendAsync("ReceiveMessage", user);
        }
    }
}
